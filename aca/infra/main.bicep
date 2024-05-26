targetScope = 'subscription'

@minLength(1)
@maxLength(64)
@description('Name of the environment that can be used as part of naming resource convention')
param environmentName string

@minLength(1)
@description('Primary location for all resources')
param location string

param chatAPIExists bool
@secure()
param chatAPIDefinition object
param userPortalExists bool
@secure()
param userPortalDefinition object

@description('Id of the user or app to assign application roles')
param principalId string

// Tags that should be applied to all resources.
// 
// Note that 'azd-service-name' tags should be applied separately to service host resources.
// Example usage:
//   tags: union(tags, { 'azd-service-name': <service name in azure.yaml> })
var tags = {
  'azd-env-name': environmentName
}

var abbrs = loadJsonContent('./abbreviations.json')
var resourceToken = toLower(uniqueString(subscription().id, environmentName, location))

resource rg 'Microsoft.Resources/resourceGroups@2022-09-01' = {
  name: 'rg-${environmentName}'
  location: location
  tags: tags
}

module monitoring './shared/monitoring.bicep' = {
  name: 'monitoring'
  params: {
    keyvaultName: keyVault.outputs.name
    location: location
    tags: tags
    logAnalyticsName: '${abbrs.operationalInsightsWorkspaces}${resourceToken}'
    applicationInsightsName: '${abbrs.insightsComponents}${resourceToken}'
  }
  scope: rg
}

module cosmos './shared/cosmosdb.bicep' = {
  name: 'cosmos'
  params: {
    containers: [
      {
        name: 'embedding'
        partitionKeyPath: '/id'
        indexingPolicy: null
        vectorEmbeddingPolicy: {}
      }
      {
        name: 'completions'
        partitionKeyPath: '/sessionId'
        indexingPolicy: null
        vectorEmbeddingPolicy: {}
      }
      {
        name: 'product'
        partitionKeyPath: '/categoryId'
        indexingPolicy: null
        vectorEmbeddingPolicy: {}
      }
      {
        name: 'customer'
        partitionKeyPath: '/customerId'
        indexingPolicy: null
        vectorEmbeddingPolicy: {}
      }
      {
        name: 'leases'
        partitionKeyPath: '/id'
        indexingPolicy: null
        vectorEmbeddingPolicy: {}
      }
    ]
    databaseName: 'vsai-database'
    keyvaultName: keyVault.outputs.name
    location: location
    name: '${abbrs.documentDBDatabaseAccounts}${resourceToken}'
    tags: tags
  }
  scope: rg
}

module cosmosVec './shared/cosmosdb.bicep' = {
  name: 'cosmosVec'
  params: {
    capabilities: [
      {
        name: 'EnableNoSQLVectorSearch'
      }
      {
        name: 'EnableServerless'
      }
    ]
    containers: [
      {
        name: 'main-vector-store'
        partitionKeyPath: '/partitionKey'
        indexingPolicy: {
          indexingMode: 'consistent'
          automatic: true
          includedPaths: [
            {
              path: '/*'
            }
          ]
          excludedPaths: [
            {
              path: '/embedding/?'
            }
          ]
          vectorIndexes: [
            {
              path: '/embedding'
              type: 'quantizedFlat'
            }
          ]
        }
        vectorEmbeddingPolicy: {
          vectorEmbeddings: [
            {
              path: '/embedding'
              dataType: 'float32'
              dimensions: 1536
              distanceFunction: 'cosine'
            }
          ]
        }
      }
      {
        name: 'cache-vector-store'
        partitionKeyPath: '/partitionKey'
        indexingPolicy: {
          indexingMode: 'consistent'
          automatic: true
          includedPaths: [
            {
              path: '/*'
            }
          ]
          vectorIndexes: [
            {
              path: '/embedding'
              type: 'quantizedFlat'
            }
          ]
        }
        vectorEmbeddingPolicy: {
          vectorEmbeddings: [
            {
              path: '/embedding'
              dataType: 'float32'
              dimensions: 1536
              distanceFunction: 'cosine'
            }
          ]
        }
      }
    ]
    databaseName: 'byoc-database'
    keyvaultName: keyVault.outputs.name
    secretName: 'cosmosdb-vec-key'
    location: location
    name: '${abbrs.documentDBDatabaseAccounts}vec${resourceToken}'
    tags: tags
  }
  scope: rg
}

module dashboard './shared/dashboard-web.bicep' = {
  name: 'dashboard'
  params: {
    name: '${abbrs.portalDashboards}${resourceToken}'
    applicationInsightsName: monitoring.outputs.applicationInsightsName
    location: location
    tags: tags
  }
  scope: rg
}

module registry './shared/registry.bicep' = {
  name: 'registry'
  params: {
    location: location
    tags: tags
    name: '${abbrs.containerRegistryRegistries}${resourceToken}'
  }
  scope: rg
}

module keyVault './shared/keyvault.bicep' = {
  name: 'keyvault'
  params: {
    location: location
    tags: tags
    name: '${abbrs.keyVaultVaults}${resourceToken}'
    principalId: principalId
  }
  scope: rg
}

module openAi './shared/openai.bicep' = {
  name: 'openai'
  params: {
    deployments: [
      {
        name: 'completions'
        sku: {
          name: 'Standard'
          capacity: 120
        }
        model: {
          name: 'gpt-35-turbo'
          version: '0613'
        }
      }
      {
        name: 'embeddings'
        sku: {
          name: 'Standard'
          capacity: 120
        }
        model: {
          name: 'text-embedding-ada-002'
          version: '2'
        }
      }
    ]
    keyvaultName: keyVault.outputs.name
    location: location
    name: '${abbrs.openAiAccounts}${resourceToken}'
    sku: 'S0'
    tags: tags
  }
  scope: rg
}

module storage './shared/storage.bicep' = {
  name: 'storage'
  params: {
    containers: [
      {
        name: 'system-prompt'
      }
      {
        name: 'memory-source'
      }
      {
        name: 'product-policy'
      }
    ]
    files: [
      {
        name: 'retailassistant-default-txt'
        file: 'Default.txt'
        path: 'RetailAssistant/Default.txt'
        content: loadTextContent('../../SystemPrompts/RetailAssistant/Default.txt')
        container: 'system-prompt'
      }
      {
        name: 'retailassistant-limited-txt'
        file: 'Limited.txt'
        path: 'RetailAssistant/Limited.txt'
        content: loadTextContent('../../SystemPrompts/RetailAssistant/Limited.txt')
        container: 'system-prompt'
      }
      {
        name: 'summarizer-twowords-txt'
        file: 'TwoWords.txt'
        path: 'Summarizer/TwoWords.txt'
        content: loadTextContent('../../SystemPrompts/Summarizer/TwoWords.txt')
        container: 'system-prompt'
      }
      {
        name: 'blobmemorysourceconfig-json'
        file: 'BlobMemorySourceConfig.json'
        path: 'BlobMemorySourceConfig.json'
        content: loadTextContent('../../MemorySources/BlobMemorySourceConfig.json')
        container: 'memory-source'
      }
      {
        name: 'return-policies-txt'
        file: 'return-policies.txt'
        path: 'return-policies.txt'
        content: loadTextContent('../../MemorySources/return-policies.txt')
        container: 'product-policy'
      }
      {
        name: 'shipping-policies-txt'
        file: 'shipping-policies.txt'
        path: 'shipping-policies.txt'
        content: loadTextContent('../../MemorySources/shipping-policies.txt')
        container: 'product-policy'
      }
    ]
    keyvaultName: keyVault.outputs.name
    location: location
    name: '${abbrs.storageStorageAccounts}${resourceToken}'
    tags: tags
  }
  scope: rg
}

module appsEnv './shared/apps-env.bicep' = {
  name: 'apps-env'
  params: {
    name: '${abbrs.appManagedEnvironments}${resourceToken}'
    location: location
    tags: tags
    applicationInsightsName: monitoring.outputs.applicationInsightsName
    logAnalyticsWorkspaceName: monitoring.outputs.logAnalyticsWorkspaceName
  }
  scope: rg
}

module chatAPI './app/ChatAPI.bicep' = {
  name: 'ChatAPI'
  params: {
    name: '${abbrs.appContainerApps}chatapi-${resourceToken}'
    location: location
    tags: tags
    cosmosDbAccountName: cosmos.outputs.name
    storageAccountName: storage.outputs.name
    identityName: '${abbrs.managedIdentityUserAssignedIdentities}chatapi-${resourceToken}'
    keyvaultName: keyVault.outputs.name
    applicationInsightsName: monitoring.outputs.applicationInsightsName
    containerAppsEnvironmentName: appsEnv.outputs.name
    containerRegistryName: registry.outputs.name
    exists: chatAPIExists
    appDefinition: chatAPIDefinition
    envSettings: [
      {
        name: 'MSCosmosDBOpenAI__CosmosDBVectorStore__Endpoint'
        value: cosmosVec.outputs.endpoint
      }
      {
        name: 'MSCosmosDBOpenAI__OpenAI__Endpoint'
        value: openAi.outputs.endpoint
      }
      {
        name: 'MSCosmosDBOpenAI__CosmosDB__Endpoint'
        value: cosmos.outputs.endpoint
      }
    ]
    secretSettings: [
      {
        name: 'ApplicationInsights__ConnectionString'
        value: monitoring.outputs.applicationInsightsConnectionSecretRef
        secretRef: monitoring.outputs.applicationInsightsConnectionSecretName
      }
      {
        name: 'MSCosmosDBOpenAI__BlobStorageMemorySource__ConfigBlobStorageConnection'
        value: storage.outputs.connectionSecretRef
        secretRef: storage.outputs.connectionSecretName
      }
      {
        name: 'MSCosmosDBOpenAI__CosmosDB__Key'
        value: cosmos.outputs.keySecretRef
        secretRef: cosmos.outputs.keySecretName
      }
      {
        name: 'MSCosmosDBOpenAI__CosmosDBVectorStore__Key'
        value: cosmosVec.outputs.keySecretRef
        secretRef: cosmosVec.outputs.keySecretName
      }
      {
        name: 'MSCosmosDBOpenAI__DurableSystemPrompt__BlobStorageConnection'
        value: storage.outputs.connectionSecretRef
        secretRef: storage.outputs.connectionSecretName
      }
      {
        name: 'MSCosmosDBOpenAI__OpenAI__Key'
        value: openAi.outputs.keySecretRef
        secretRef: openAi.outputs.keySecretName
      }
    ]
  }
  scope: rg
  dependsOn: [ cosmos, monitoring, openAi, storage ]
}

module userPortal './app/UserPortal.bicep' = {
  name: 'UserPortal'
  params: {
    apiUri: chatAPI.outputs.uri
    name: '${abbrs.appContainerApps}userportal-${resourceToken}'
    location: location
    tags: tags
    keyvaultName: keyVault.outputs.name
    identityName: '${abbrs.managedIdentityUserAssignedIdentities}userportal-${resourceToken}'
    applicationInsightsName: monitoring.outputs.applicationInsightsName
    containerAppsEnvironmentName: appsEnv.outputs.name
    containerRegistryName: registry.outputs.name
    exists: userPortalExists
    appDefinition: userPortalDefinition
    envSettings: [
      {
        name: 'MSCosmosDBOpenAI__ChatManager__APIRoutePrefix'
        value: ''
      }
      {
        name: 'MSCosmosDBOpenAI__ChatManager__APIUrl'
        value: chatAPI.outputs.uri
      }
    ]
    secretSettings: [
      {
        name: 'ApplicationInsights__ConnectionString'
        value: monitoring.outputs.applicationInsightsConnectionSecretRef
        secretRef: monitoring.outputs.applicationInsightsConnectionSecretName
      }
    ]
  }
  scope: rg
  dependsOn: [ chatAPI, monitoring ]
}

output AZURE_CONTAINER_REGISTRY_ENDPOINT string = registry.outputs.loginServer
output AZURE_COSMOS_DB_NAME string = cosmos.outputs.name
output AZURE_COSMOS_DB_VEC_NAME string = cosmosVec.outputs.name
output AZURE_KEY_VAULT_NAME string = keyVault.outputs.name
output AZURE_KEY_VAULT_ENDPOINT string = keyVault.outputs.endpoint
output AZURE_STORAGE_ACCOUNT_NAME string = storage.outputs.name

output SERVICE_CHATAPI_ENDPOINT_URL string = chatAPI.outputs.uri
