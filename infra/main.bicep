// ============================================
// AI Document Processor - Infrastructure
// ============================================

targetScope = 'resourceGroup'

@description('Base name for all resources')
param baseName string = 'docprocessor'

@description('Azure region')
param location string = resourceGroup().location

@description('SQL Server admin username')
@secure()
param sqlAdminLogin string

@description('SQL Server admin password')
@secure()
param sqlAdminPassword string

@description('App Service Plan SKU')
param appServiceSku string = 'B1'

// ---- Storage Account ----
resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: '${replace(baseName, '-', '')}storage'
  location: location
  sku: { name: 'Standard_LRS' }
  kind: 'StorageV2'
  properties: {
    accessTier: 'Hot'
    supportsHttpsTrafficOnly: true
    minimumTlsVersion: 'TLS1_2'
  }
}

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-05-01' = {
  parent: storageAccount
  name: 'default'
}

resource documentsContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: blobService
  name: 'documents'
  properties: { publicAccess: 'None' }
}

// ---- SQL Server & Database ----
resource sqlServer 'Microsoft.Sql/servers@2023-08-01-preview' = {
  name: '${baseName}-sql'
  location: location
  properties: {
    administratorLogin: sqlAdminLogin
    administratorLoginPassword: sqlAdminPassword
    minimalTlsVersion: '1.2'
  }
}

resource sqlDatabase 'Microsoft.Sql/servers/databases@2023-08-01-preview' = {
  parent: sqlServer
  name: '${baseName}-db'
  location: location
  sku: { name: 'Basic', tier: 'Basic' }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    maxSizeBytes: 2147483648 // 2GB
  }
}

resource sqlFirewallAllowAzure 'Microsoft.Sql/servers/firewallRules@2023-08-01-preview' = {
  parent: sqlServer
  name: 'AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

// ---- Document Intelligence ----
resource docIntelligence 'Microsoft.CognitiveServices/accounts@2024-10-01' = {
  name: '${baseName}-docintel'
  location: location
  kind: 'FormRecognizer'
  sku: { name: 'S0' }
  properties: {
    customSubDomainName: '${baseName}-docintel'
    publicNetworkAccess: 'Enabled'
  }
}

// ---- Key Vault ----
resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: '${baseName}-kv'
  location: location
  properties: {
    sku: { family: 'A', name: 'standard' }
    tenantId: subscription().tenantId
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 7
  }
}

// Store secrets in Key Vault
resource secretSqlConn 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'SqlConnectionString'
  properties: {
    value: 'Server=tcp:${sqlServer.properties.fullyQualifiedDomainName},1433;Database=${sqlDatabase.name};User ID=${sqlAdminLogin};Password=${sqlAdminPassword};Encrypt=True;TrustServerCertificate=False;'
  }
}

resource secretBlobConn 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'BlobConnectionString'
  properties: {
    value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${storageAccount.listKeys().keys[0].value};EndpointSuffix=core.windows.net'
  }
}

resource secretDocIntelKey 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'DocIntelligenceKey'
  properties: {
    value: docIntelligence.listKeys().key1
  }
}

// ---- App Service ----
resource appServicePlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: '${baseName}-plan'
  location: location
  sku: { name: appServiceSku }
  kind: 'linux'
  properties: { reserved: true }
}

resource apiApp 'Microsoft.Web/sites@2023-12-01' = {
  name: '${baseName}-api'
  location: location
  identity: { type: 'SystemAssigned' }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNET|10.0'
      alwaysOn: true
      appSettings: [
        { name: 'Azure__DocumentIntelligence__Endpoint', value: docIntelligence.properties.endpoint }
        { name: 'Azure__DocumentIntelligence__Key', value: '@Microsoft.KeyVault(VaultName=${keyVault.name};SecretName=DocIntelligenceKey)' }
        { name: 'Azure__BlobStorage__ConnectionString', value: '@Microsoft.KeyVault(VaultName=${keyVault.name};SecretName=BlobConnectionString)' }
        { name: 'Azure__BlobStorage__ContainerName', value: 'documents' }
        { name: 'ConnectionStrings__DefaultConnection', value: '@Microsoft.KeyVault(VaultName=${keyVault.name};SecretName=SqlConnectionString)' }
      ]
    }
  }
}

// Grant API app access to Key Vault secrets
resource kvRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: keyVault
  name: guid(keyVault.id, apiApp.id, '4633458b-17de-408a-b874-0445c86b69e6')
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6') // Key Vault Secrets User
    principalId: apiApp.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

// ---- Outputs ----
output apiUrl string = 'https://${apiApp.properties.defaultHostName}'
output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName
output storageAccountName string = storageAccount.name
output docIntelligenceEndpoint string = docIntelligence.properties.endpoint
output keyVaultName string = keyVault.name
