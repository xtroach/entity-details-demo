// Parameters for the staging environment. The images change on every deploy, so they come from the
// API_IMAGE and CLIENT_IMAGE environment variables, set by the deploy workflow (see README
// "Deploying (staging)"). There's deliberately no default: building or deploying without them fails.
using 'main.bicep'

param environmentName = 'staging'
param aspNetCoreEnvironment = 'Staging'
param apiImage = readEnvironmentVariable('API_IMAGE')
param clientImage = readEnvironmentVariable('CLIENT_IMAGE')

// Optional: your own Entra login as a second database admin, for debugging with psql. Set both
// variables when deploying by hand; the workflow leaves them empty.
param postgresAdminUserObjectId = readEnvironmentVariable('POSTGRES_ADMIN_USER_OBJECT_ID', '')
param postgresAdminUserPrincipalName = readEnvironmentVariable('POSTGRES_ADMIN_USER_PRINCIPAL_NAME', '')
