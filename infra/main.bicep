@description('Name of the storage account.')
param storageAccountName string = 'sa${uniqueString(resourceGroup().id)}'

@description('Name of the AI Search instance.')
param aiSearchName string = 'aisearch-${uniqueString(resourceGroup().id)}'

@description('Name of the Azure OpenAI instance.')
param azureOpenAIName string = 'openai-${uniqueString(resourceGroup().id)}'

@description('Name of the blob storage container that contains the documents for the index.')
param blobStorageContainerName string = 'documents'

@description('Name of the embedding model to use.')
param embeddingModelName string = 'text-embedding-3-large'

@description('Name/id of the embedding model to be used for the indexer during indexing.')
param indexerEmbeddingModelId string = 'text-embedding-3-large-indexer'

@description('Name of completionmodel.')
param completionModelName string = 'gpt-4o'

@description('API Version of completion model.')
param completionModelAPIVersion string = '2024-08-06'

@description('Name/id of the embedding model to be used for the indexer during querying.')
param integratedVectorEmbeddingModelId string = 'text-embedding-3-large-aisearchquery'

@description('Name of the AI search index to be created or updated, must be lowercase.')
param indexName string = 'onyourdata'

@description('Name of the app service plan.')
param aspName string = 'asp-${uniqueString(resourceGroup().id)}'

@description('Name of the back end site.')
param backendAppName string = 'backend-${uniqueString(resourceGroup().id)}'

@description('Name of the front end site.')
param frontendAppName string = 'frontend-${uniqueString(resourceGroup().id)}'

@description('Name of the function app.')
param functionAppName string = 'func-${uniqueString(resourceGroup().id)}'

@maxLength(24)
@description('Name of the storage account for the function app.')
param functionAppStorageAccountName string = take('sa${uniqueString(resourceGroup().id)}func', 20)

@description('Name of the cosmos DB account.')
param cosmosAccountName string = 'cosmos-${uniqueString(resourceGroup().id)}'

@description('Azure AD instance for backend api security.')
param azureAdInstance string = 'https://login.microsoftonline.com'

@description('Client ID of the app registration of the backend app.')
param azureAdClientId string

@description('Tenant ID.')
param azureAdTenantId string

var cosmosDatabaseName = 'history'
var cosmosDocumentContainerName = 'documentsperthread'
var cosmosDocumentContainerLeaseName = 'docsleases'
var cosmosHistoryContainerName = 'threadhistory'
var cosmosThreadContainerLeaseName = 'threadleases'
var sqlRoleName = 'sql-contributor-${cosmosAccountName}'
var blobOwnerRoleId = 'b7e6dc6d-f1e8-4753-8033-0f276bb0955b'

module appServicePlan 'br/public:avm/res/web/serverfarm:0.3.0' = {
  name: 'appServicePlan'
  params: {
    name: aspName
    location: resourceGroup().location
    skuName: 'B1'
  }
}

module frontendSite 'br/public:avm/res/web/site:0.9.0' = {
  name: 'frontendSite'
  params: {
    kind: 'app'
    name: frontendAppName
    serverFarmResourceId: appServicePlan.outputs.resourceId
    location: resourceGroup().location
  }
}

module backendSite 'br/public:avm/res/web/site:0.9.0' = {
  name: 'backendSite'
  params: {
    kind: 'app'
    name: backendAppName
    serverFarmResourceId: appServicePlan.outputs.resourceId
    location: resourceGroup().location
    managedIdentities: {
      systemAssigned: true
    }
    siteConfig: {
      cors: {
        allowedOrigins: [
          'https://${frontendSite.outputs.defaultHostname}'
        ]
        supportCredentials: true
      }
    }
  }
}

module functionAppStorageAccount 'br/public:avm/res/storage/storage-account:0.9.1' = {
  name: 'functionAppStorageAccount'
  params: {
    name: functionAppStorageAccountName
    kind: 'BlobStorage'
    location: resourceGroup().location
    skuName: 'Standard_GRS'
    allowBlobPublicAccess: false
    publicNetworkAccess: 'Enabled'
    networkAcls: {
      bypass: 'AzureServices'
      defaultAction: 'Allow'
    }
  }
}

module workspace 'br/public:avm/res/operational-insights/workspace:0.7.1' = {
  name: 'workspaceDeployment'
  params: {
    name: functionAppName
    location: resourceGroup().location
  }
}
module appInsightsFuntionApp 'br/public:avm/res/insights/component:0.4.1' = {
  name: 'appInsightsFuntionApp'
  params: {
    name: functionAppName
    workspaceResourceId: workspace.outputs.resourceId
    location: resourceGroup().location
  }
}

module functionApp 'br/public:avm/res/web/site:0.9.0' = {
  name: 'functionApp'
  params: {
    kind: 'functionapp'
    name: functionAppName
    serverFarmResourceId: appServicePlan.outputs.resourceId
    location: resourceGroup().location
    managedIdentities: {
      systemAssigned: true
    }
    siteConfig: {
      use32BitWorkerProcess: false
      netFrameworkVersion: 'v8.0'
      alwaysOn: true
    }
    appInsightResourceId: appInsightsFuntionApp.outputs.resourceId
    storageAccountRequired: true
    storageAccountResourceId: functionAppStorageAccount.outputs.resourceId
    storageAccountUseIdentityAuthentication: true
  }
}

resource roleDefinitionBlobContributor 'Microsoft.Authorization/roleDefinitions@2022-04-01' existing = {
  scope: resourceGroup()
  name: blobOwnerRoleId
}

resource functionAppStorageAccountExisting 'Microsoft.Storage/storageAccounts@2023-05-01' existing = {
  name: functionAppStorageAccountName
  scope: resourceGroup()
}
resource functionAppBlobRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
    scope: functionAppStorageAccountExisting
    name: guid(subscription().id, resourceGroup().id, functionApp.name, blobOwnerRoleId)
    properties: {
      roleDefinitionId: roleDefinitionBlobContributor.id
      principalId: functionApp.outputs.systemAssignedMIPrincipalId
      principalType: 'ServicePrincipal'
    } 
}

module aiSearch 'br/public:avm/res/search/search-service:0.7.1' = {
  name: 'aiSearch'
  params: {
    name: aiSearchName
    sku: 'basic'
    location: resourceGroup().location
    managedIdentities: {
      systemAssigned: true
    }
    replicaCount: 1
    partitionCount: 1
    roleAssignments: [
      {
        principalId: backendSite.outputs.systemAssignedMIPrincipalId
        principalType: 'ServicePrincipal'
        roleDefinitionIdOrName: 'Search Index Data Contributor'
      }
      {
        principalId: functionApp.outputs.systemAssignedMIPrincipalId
        principalType: 'ServicePrincipal'
        roleDefinitionIdOrName: 'Search Index Data Contributor'
      }
      {
        principalId: backendSite.outputs.systemAssignedMIPrincipalId
        principalType: 'ServicePrincipal'
        roleDefinitionIdOrName: 'Search Service Contributor'
      }
    ]
  }
}

module storageAccount 'br/public:avm/res/storage/storage-account:0.9.1' = {
  name: 'storageAccount'
  params: {
    name: storageAccountName
    kind: 'BlobStorage'
    allowSharedKeyAccess:false
    location: resourceGroup().location
    skuName: 'Standard_GRS'
    allowBlobPublicAccess: false
    publicNetworkAccess: 'Enabled'
    networkAcls: {
      bypass: 'AzureServices'
      defaultAction: 'Allow'
    }
    blobServices: {
      containers: [
        {
          name: blobStorageContainerName
          roleAssignments: [
            {
              principalId: aiSearch.outputs.systemAssignedMIPrincipalId
              principalType: 'ServicePrincipal'
              roleDefinitionIdOrName: 'Storage Blob Data Reader'
            }
            {
              principalId: backendSite.outputs.systemAssignedMIPrincipalId
              principalType: 'ServicePrincipal'
              roleDefinitionIdOrName: 'Storage Blob Data Contributor'
            }
            {
              principalId: functionApp.outputs.systemAssignedMIPrincipalId
              principalType: 'ServicePrincipal'
              roleDefinitionIdOrName: 'Storage Blob Data Contributor'
            }
          ]
        }
      ]
    }
  }
}

module openAi './modules/cognitiveservices/cognitive-services.bicep' = {
  name: 'openai'
  params: {
    name: azureOpenAIName
    deployments: [
      {
        model: {
          format: 'OpenAI'
          name: embeddingModelName
          version: '1'
        }
        name: indexerEmbeddingModelId
      }
      {
        model: {
          format: 'OpenAI'
          name: embeddingModelName
          version: '1'
        }
        name: integratedVectorEmbeddingModelId
      }
      {
        model: {
          format: 'OpenAI'
          name: completionModelName
          version: completionModelAPIVersion
        }
        name: completionModelName
      }
    ]
    roleAssignmentPrincipalIds: [
      aiSearch.outputs.systemAssignedMIPrincipalId
      backendSite.outputs.systemAssignedMIPrincipalId
    ]
  }
}

module aiSearchIndex 'modules/aisearchindex/ai-search-index.bicep' = {
  name: 'aiSearchIndex'
  params: {
    indexName: indexName
    aiSearchName: aiSearch.outputs.name
    storageAccountContainerName: blobStorageContainerName
    storageAccountResourceId: storageAccount.outputs.resourceId
    embeddingModelName: embeddingModelName
    integratedVectorEmbeddingModelId: integratedVectorEmbeddingModelId
    indexerEmbeddingModelId: indexerEmbeddingModelId
    azureOpenAIEndpoint: openAi.outputs.endpoint
  }
}

module cosmosDB 'br/public:avm/res/document-db/database-account:0.8.0' = {
  name: 'cosmosDB'
  params: {
    name: cosmosAccountName
    location: 'francecentral'
    networkRestrictions: {
      publicNetworkAccess: 'Enabled'
      ipRules: []
      virtualNetworkRules: []
    }
    sqlDatabases: [
      {
        name: cosmosDatabaseName
        containers: [
          {
            indexingPolicy: {
              automatic: true
            }
            name: cosmosDocumentContainerName
            paths: [
              '/userId'
            ]
          }
          {
            indexingPolicy: {
              automatic: true
            }
            name: cosmosHistoryContainerName
            paths: [
              '/userId'
            ]
          }
          {
            indexingPolicy: {
              automatic: true
            }
            name: cosmosDocumentContainerLeaseName
            paths: [
              '/id'
            ]
          }
          {
            indexingPolicy: {
              automatic: true
            }
            name: cosmosThreadContainerLeaseName
            paths: [
              '/id'
            ]
          }
        ]
      }
    ]
    sqlRoleAssignmentsPrincipalIds: [
      backendSite.outputs.systemAssignedMIPrincipalId
      functionApp.outputs.systemAssignedMIPrincipalId
    ]
    sqlRoleDefinitions: [
      {
        name: sqlRoleName
        dataAction: [
          'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers/write'
          'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers/read'
          'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers/delete'
        ]
      }
    ]
  }
}

resource existingBackend 'Microsoft.Web/sites@2023-12-01' existing = {
  name: backendAppName
  scope: resourceGroup()
}

resource backendAppSettings 'Microsoft.Web/sites/config@2022-09-01' = {
  name: 'appsettings'
  kind: 'string'
  parent: existingBackend
  properties: {
    'AzureAd:Instance': azureAdInstance
    'AzureAd:ClientId': azureAdClientId
    'AzureAd:TenantId': azureAdTenantId
    'Storage:ServiceUri': storageAccount.outputs.primaryBlobEndpoint
    'Storage:AccountName': storageAccount.outputs.name
    'Storage:ContainerName': blobStorageContainerName
    'Cosmos:AccountEndpoint': cosmosDB.outputs.endpoint
    'Cosmos:DatabaseName': cosmosDatabaseName
    'Cosmos:ContainerName': cosmosDocumentContainerName
    'Cosmos:ThreadHistoryContainerName': cosmosHistoryContainerName
    'Search:EndPoint': 'https://${aiSearch.outputs.name}.search.windows.net'
    'Search:IndexName': indexName
    'Search:IndexerName': '${indexName}-indexer'
    'Search:DataSourceName': '${indexName}-datasource'
    'OpenAI:EndPoint': openAi.outputs.endpoint
    'OpenAI:EmbeddingModel': integratedVectorEmbeddingModelId
    'OpenAI:CompletionModel': completionModelName
    'OpenAI:ReasoningModel': completionModelName
    APPLICATIONINSIGHTS_CONNECTION_STRING: appInsightsFuntionApp.outputs.connectionString
    'SPE:ContainerTypeId': ''
    Settings:'{"AllowInitialPromptRewrite":false,"AllowFollowUpPrompts":true,"UseSemanticRanker":false,"AllowInitialPromptToHelpUser":true,"PredefinedPrompts":[{"Id":"1","Name":"Default","Prompt":"You are a helpful assistant."},{"Id":"2","Name":"Default with Semantic Ranker","Prompt":"You are a helpful assistant. Use semantic ranker to find the most relevant document."}],"Seed":0,"Temperature":1.0}'
  }
}

resource existingFunction 'Microsoft.Web/sites@2023-12-01' existing = {
  name: functionAppName
  scope: resourceGroup()
}

resource functionAppSettings 'Microsoft.Web/sites/config@2022-09-01' = {
  name: 'appsettings'
  kind: 'string'
  parent: existingFunction
  properties: {
    FUNCTIONS_EXTENSION_VERSION: '~4'
    WEBSITE_RUN_FROM_PACKAGE: '1'
    APPINSIGHTS_INSTRUMENTATIONKEY: appInsightsFuntionApp.outputs.instrumentationKey
    APPLICATIONINSIGHTS_CONNECTION_STRING: appInsightsFuntionApp.outputs.connectionString
    AzureWebJobsStorage__accountname: functionAppStorageAccount.outputs.name
    FUNCTIONS_WORKER_RUNTIME: 'dotnet-isolated'
    CosmosDbDatabase: cosmosDatabaseName
    CosmosDbDocumentContainer: cosmosDocumentContainerName
    CosmosDbThreadContainer: cosmosHistoryContainerName
    CosmosDbDocumentLease: cosmosDocumentContainerLeaseName
    CosmosDbThreadLease: cosmosThreadContainerLeaseName
    CosmosDbConnection__accountEndpoint: cosmosDB.outputs.endpoint
    MyCosmosConnection: cosmosDB.outputs.endpoint
    ThreadCleanupCron: '0 0 * * * *'
    AZURE_TENANT_ID: azureAdTenantId
    StorageServiceUri: storageAccount.outputs.primaryBlobEndpoint
    StorageAccountName: storageAccount.outputs.name
    StorageContainerName: blobStorageContainerName
    SearchEndPoint: 'https://${aiSearch.outputs.name}.search.windows.net'
    SearchIndexName: indexName
    SearchIndexerName: '${indexName}-indexer'
    SearchDataSourceName: '${indexName}-datasource'
  }
}

output aiSearchName string = aiSearch.outputs.name
output indexerName string = aiSearchIndex.outputs.indexerName
output functionName string = functionApp.outputs.name
output backendAppName string = backendSite.outputs.name
output backendAppUrl string = 'https://${backendSite.outputs.defaultHostname}'
output frontendAppName string = frontendSite.outputs.name
