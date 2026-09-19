// Azure infrastructure for one environment of entity-details-demo: Azure Container Apps for the API,
// the Blazor client and the migration job, and Azure Database for PostgreSQL Flexible Server with
// Microsoft Entra ID authentication only (no database password exists).
//
// Deployed at resource-group scope by the deploy workflow (see README "Deploying (staging)"); the
// resource group and GitHub's deploy identity are created once, outside this template.
targetScope = 'resourceGroup'

@description('Short environment name used in every resource name, e.g. "staging". Container Apps names are limited to 32 characters.')
@minLength(2)
@maxLength(8)
param environmentName string

@description('ASP.NET Core environment for the API and the migration job.')
param aspNetCoreEnvironment string

@description('Azure region for all resources.')
param location string = resourceGroup().location

@description('API image by digest, e.g. ghcr.io/xtroach/entity-details-demo/api@sha256:... Also used by the migration job.')
@minLength(1)
param apiImage string

@description('Blazor client image by digest, e.g. ghcr.io/xtroach/entity-details-demo/client@sha256:...')
@minLength(1)
param clientImage string

@description('Optional: object ID of a Microsoft Entra user to add as a second database admin, for debugging. Empty adds none.')
param postgresAdminUserObjectId string = ''

@description('User principal name matching postgresAdminUserObjectId; required when that is set.')
param postgresAdminUserPrincipalName string = ''

@description('Maximum API replicas. A cost cap: staging is publicly writable until authentication exists (#20).')
@minValue(1)
param apiMaxReplicas int = 2

@description('Maximum client replicas.')
@minValue(1)
param clientMaxReplicas int = 1

var prefix = 'entitydetails'
var databaseName = 'entitydetails'
var apiAppName = 'ca-${prefix}-api-${environmentName}'
var clientAppName = 'ca-${prefix}-client-${environmentName}'
var migrationJobName = 'caj-${prefix}-mig-${environmentName}'
var workloadProfileName = 'Consumption'

// Both URLs derive from the environment's default domain, which exists before either app, so the
// API and the client can reference each other without a dependency cycle.
var apiUrl = 'https://${apiAppName}.${containerAppsEnvironment.properties.defaultDomain}'
var clientUrl = 'https://${clientAppName}.${containerAppsEnvironment.properties.defaultDomain}'

// Shared by the API and the migration job. The connection string carries no password: with Entra
// authentication on, the API's managed identity signs in with an access token.
var databaseSettings = [
  { name: 'ASPNETCORE_ENVIRONMENT', value: aspNetCoreEnvironment }
  {
    name: 'ConnectionStrings__AppDbContext'
    value: 'Host=${postgres.properties.fullyQualifiedDomainName};Database=${databaseName};Username=${apiIdentity.name};Ssl Mode=Require'
  }
  { name: 'Database__UseEntraAuthentication', value: 'true' }
  { name: 'Database__ManagedIdentityClientId', value: apiIdentity.properties.clientId }
  // Migrations run once per deploy, in the migration job, before the new version takes traffic.
  { name: 'Database__MigrateOnStartup', value: 'false' }
]

resource logs 'Microsoft.OperationalInsights/workspaces@2025-07-01' = {
  name: 'log-${prefix}-${environmentName}'
  location: location
  properties: {
    sku: { name: 'PerGB2018' }
    retentionInDays: 30
  }
}

resource containerAppsEnvironment 'Microsoft.App/managedEnvironments@2025-07-01' = {
  name: 'cae-${prefix}-${environmentName}'
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logs.properties.customerId
        sharedKey: logs.listKeys().primarySharedKey
      }
    }
    workloadProfiles: [
      { name: workloadProfileName, workloadProfileType: 'Consumption' }
    ]
  }
}

// The identity the API and the migration job sign in to PostgreSQL with.
resource apiIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2024-11-30' = {
  name: 'id-${prefix}-api-${environmentName}'
  location: location
}

resource postgres 'Microsoft.DBforPostgreSQL/flexibleServers@2025-08-01' = {
  // Server names are globally unique DNS names.
  name: 'psql-${prefix}-${environmentName}-${uniqueString(resourceGroup().id)}'
  location: location
  sku: {
    name: 'Standard_B1ms'
    tier: 'Burstable'
  }
  properties: {
    // Matches docker-compose.yml and the Testcontainers image.
    version: '18'
    storage: {
      storageSizeGB: 32
      autoGrow: 'Disabled'
    }
    backup: {
      backupRetentionDays: 7
      geoRedundantBackup: 'Disabled'
    }
    highAvailability: {
      mode: 'Disabled'
    }
    authConfig: {
      activeDirectoryAuth: 'Enabled'
      passwordAuth: 'Disabled'
      tenantId: tenant().tenantId
    }
    network: {
      publicNetworkAccess: 'Enabled'
    }
  }
}

// The server rejects concurrent changes to its child resources, so they're created one after
// another (each dependsOn the previous).
resource database 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2025-08-01' = {
  parent: postgres
  name: databaseName
  properties: {
    charset: 'UTF8'
    collation: 'en_US.utf8'
  }
}

// Container Apps' outbound addresses aren't fixed, so Azure services may connect at the network
// level. Signing in still needs an Entra token for an admin identity, over TLS.
resource allowAzureServices 'Microsoft.DBforPostgreSQL/flexibleServers/firewallRules@2025-08-01' = {
  parent: postgres
  name: 'AllowAllAzureServicesAndResourcesWithinAzureIps'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
  dependsOn: [database]
}

// The API's identity is a database admin so the migration job can change the schema. Production
// should split this into a migrator (admin) and a runtime identity with data access only.
module apiDatabaseAdmin 'postgres-entra-admin.bicep' = {
  name: 'postgres-admin-api'
  params: {
    serverName: postgres.name
    principalObjectId: apiIdentity.properties.principalId
    principalName: apiIdentity.name
    principalType: 'ServicePrincipal'
  }
  dependsOn: [allowAzureServices]
}

module userDatabaseAdmin 'postgres-entra-admin.bicep' = if (!empty(postgresAdminUserObjectId)) {
  name: 'postgres-admin-user'
  params: {
    serverName: postgres.name
    principalObjectId: postgresAdminUserObjectId
    principalName: postgresAdminUserPrincipalName
    principalType: 'User'
  }
  dependsOn: [apiDatabaseAdmin]
}

resource api 'Microsoft.App/containerApps@2025-07-01' = {
  name: apiAppName
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: { '${apiIdentity.id}': {} }
  }
  properties: {
    environmentId: containerAppsEnvironment.id
    workloadProfileName: workloadProfileName
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: true
        targetPort: 8080
        transport: 'auto'
        allowInsecure: false
      }
    }
    template: {
      containers: [
        {
          name: 'api'
          image: apiImage
          resources: {
            cpu: json('0.25')
            memory: '0.5Gi'
          }
          env: concat(databaseSettings, [
            { name: 'BlazorClientOrigins__0', value: clientUrl }
          ])
          probes: [
            {
              type: 'Startup'
              httpGet: { path: '/health/live', port: 8080 }
              periodSeconds: 3
              failureThreshold: 20
            }
            {
              type: 'Liveness'
              httpGet: { path: '/health/live', port: 8080 }
              periodSeconds: 10
              failureThreshold: 3
            }
            {
              type: 'Readiness'
              httpGet: { path: '/health/ready', port: 8080 }
              periodSeconds: 10
              timeoutSeconds: 5
              failureThreshold: 3
            }
          ]
        }
      ]
      scale: {
        minReplicas: 0
        maxReplicas: apiMaxReplicas
      }
    }
  }
  dependsOn: [apiDatabaseAdmin]
}

resource client 'Microsoft.App/containerApps@2025-07-01' = {
  name: clientAppName
  location: location
  properties: {
    environmentId: containerAppsEnvironment.id
    workloadProfileName: workloadProfileName
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: true
        targetPort: 8080
        transport: 'auto'
        allowInsecure: false
      }
    }
    template: {
      containers: [
        {
          name: 'client'
          image: clientImage
          resources: {
            cpu: json('0.25')
            memory: '0.5Gi'
          }
          env: [
            { name: 'API_BASE_URL', value: apiUrl }
          ]
          probes: [
            {
              type: 'Liveness'
              httpGet: { path: '/', port: 8080 }
              periodSeconds: 10
              failureThreshold: 3
            }
            {
              type: 'Readiness'
              httpGet: { path: '/', port: 8080 }
              periodSeconds: 10
              failureThreshold: 3
            }
          ]
        }
      ]
      scale: {
        minReplicas: 0
        maxReplicas: clientMaxReplicas
      }
    }
  }
}

// Runs the API image in --migrate mode once per deploy, before the new version rolls out.
resource migrationJob 'Microsoft.App/jobs@2025-07-01' = {
  name: migrationJobName
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: { '${apiIdentity.id}': {} }
  }
  properties: {
    environmentId: containerAppsEnvironment.id
    workloadProfileName: workloadProfileName
    configuration: {
      triggerType: 'Manual'
      replicaTimeout: 600
      replicaRetryLimit: 0
      manualTriggerConfig: {
        parallelism: 1
        replicaCompletionCount: 1
      }
    }
    template: {
      containers: [
        {
          name: 'migrate'
          image: apiImage
          // Appended to the image's ENTRYPOINT: dotnet EntityDetails.Api.dll --migrate
          args: ['--migrate']
          resources: {
            cpu: json('0.25')
            memory: '0.5Gi'
          }
          env: databaseSettings
        }
      ]
    }
  }
  dependsOn: [apiDatabaseAdmin]
}

output apiUrl string = apiUrl
output clientUrl string = clientUrl
output apiAppName string = api.name
output clientAppName string = client.name
output migrationJobName string = migrationJob.name
output postgresServerName string = postgres.name
