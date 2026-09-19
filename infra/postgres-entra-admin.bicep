// Adds one Microsoft Entra ID principal as an admin of a PostgreSQL Flexible Server.
//
// A module rather than a resource in main.bicep, because the admin resource is named after the
// principal's object ID. For a managed identity created in the same deployment, that ID is only
// known once the identity exists, and a resource name must be known when the deployment starts; a
// module parameter can carry a runtime value.
targetScope = 'resourceGroup'

@description('Name of the existing PostgreSQL Flexible Server.')
param serverName string

@description('Object (principal) ID of the Entra principal.')
param principalObjectId string

@description('Display name of the principal: for a managed identity, its resource name. It becomes the PostgreSQL role name.')
param principalName string

@description('Kind of principal.')
@allowed(['ServicePrincipal', 'User', 'Group'])
param principalType string

resource server 'Microsoft.DBforPostgreSQL/flexibleServers@2025-08-01' existing = {
  name: serverName
}

resource admin 'Microsoft.DBforPostgreSQL/flexibleServers/administrators@2025-08-01' = {
  parent: server
  name: principalObjectId
  properties: {
    principalType: principalType
    principalName: principalName
    tenantId: tenant().tenantId
  }
}
