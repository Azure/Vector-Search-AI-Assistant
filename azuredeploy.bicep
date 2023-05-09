@description('Location where all resources will be deployed. This value defaults to the **East US** region.')
@allowed([
  'South Central US'
  'East US'
  'West Europe'
])
param location string = 'East US'

@description('''
Unique name for the deployed services below. Max length 15 characters, alphanumeric only:
- Azure Cosmos DB
- Azure App Service
- Azure Functions
- Azure OpenAI
- Redis Enterprise
The name defaults to a unique string generated from the resource group identifier.
''')
@maxLength(15)
param name string = uniqueString(resourceGroup().id)

@description('Specifies the SKU for the Azure App Service plan. Defaults to **B1**')
@allowed([
  'B1'
  'S1'
])
param appServiceSku string = 'B1'

@description('Specifies the SKU for the Azure OpenAI resource. Defaults to **S0**')
@allowed([
  'S0'
])
param openAiSku string = 'S0'

@description('Git repository URL for the application source. This defaults to the [`AzureCosmosDB/VectorSearchAiAssistant`](https://github.com/AzureCosmosDB/VectorSearchAiAssistant) repository.')
param appGitRepository string = 'https://github.com/AzureCosmosDB/VectorSearchAiAssistant.git'

@description('Git repository branch for the application source. This defaults to the [**main** branch of the `AzureCosmosDB/VectorSearchAiAssistant`](https://github.com/AzureCosmosDB/VectorSearchAiAssistant/tree/main) repository.')
param appGetRepositoryBranch string = 'main'

var openAiSettings = {
  name: '${name}-openai'
  sku: openAiSku
  maxConversationBytes: '2000'
  completionsModel: {
    name: 'gpt-35-turbo'
    version: '0301'
    deployment: {
      name: '${name}-completions'
    }
  }
  embeddingsModel: {
    name: 'text-embedding-ada-002'
    version: '2'
    deployment: {
      name: '${name}-embeddings'
    }
  }
}

var cosmosDbSettings = {
  name: '${name}-cosmos-nosql'
  databaseName: 'database'
}

var cosmosContainers = {
  embeddingContainer: {
    name: 'embedding'
    partitionKeyPath : '/id'
    maxThroughput: 1000
  }
  completionsContainer: {
    name: 'completions'
    partitionKeyPath: '/sessionId'
    maxThroughput: 1000
  }
  productContainer: {
    name: 'product'
    partitionKeyPath: '/categoryId'
    maxThroughput: 1000
  }
  customerContainer: {
    name: 'customer'
    partitionKeyPath: '/customerId'
    maxThroughput: 1000
  }
  leasesContainer: {
    name: 'leases'
    partitionKeyPath: '/id'
    maxThroughput: 1000
  }
}

var appServiceSettings = {
  plan: {
    name: '${name}-web-plan'
    sku: appServiceSku
  }
  web: {
    name: '${name}-web'
    git: {
      repo: appGitRepository
      branch: appGetRepositoryBranch
    }
  }
  function: {
    name: '${name}-function'
    git: {
      repo: appGitRepository
      branch: appGetRepositoryBranch
    }
  }
}

var redisSettings = {
  name: '${name}-redis'
  database : {
    name: 'default'
  }
}

resource redisEnterprise 'Microsoft.Cache/redisEnterprise@2022-01-01' = {
  name: redisSettings.name
  location: location
  sku: {
    name: 'Enterprise_E10'
    capacity: 2
  }
  properties: {
    minimumTlsVersion: '1.2'
  }
}

resource redisEnterpriseDatabase 'Microsoft.Cache/redisEnterprise/databases@2022-01-01' = {
  parent: redisEnterprise
  name: redisSettings.database.name
  properties: {
    clientProtocol: 'Encrypted'
    port: 10000
    clusteringPolicy: 'EnterpriseCluster'
    evictionPolicy: 'NoEviction'
    modules: [
      {
        name: 'RediSearch'
      }
    ]
    persistence: {
      aofEnabled: false
      rdbEnabled: false
    }
  }
}

resource cosmosDbAccount 'Microsoft.DocumentDB/databaseAccounts@2022-08-15' = {
  name: cosmosDbSettings.name
  location: location
  kind: 'GlobalDocumentDB'
  properties: {
    consistencyPolicy: {
      defaultConsistencyLevel: 'Session'
    }
    databaseAccountOfferType: 'Standard'
    locations: [
      {
        failoverPriority: 0
        isZoneRedundant: false
        locationName: location
      }
    ]
  }
}

resource cosmosDatabase 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2022-08-15' = {
  parent: cosmosDbAccount
  name: cosmosDbSettings.databaseName
  properties: {
    resource: {
      id: cosmosDbSettings.databaseName
    }
  }
}

resource cosmosContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2022-08-15' = [for container in items(cosmosContainers):  {
  parent: cosmosDatabase
  name: container.value.name
  properties: {
    resource: {
      id: container.value.name
      partitionKey: {
        paths: [
          container.value.partitionKeyPath
        ]
        kind: 'Hash'
        version: 2
      }
    }
    options: {
      autoscaleSettings: {
        maxThroughput: container.value.maxThroughput
      }
    }
  }
}]


resource openAiAccount 'Microsoft.CognitiveServices/accounts@2022-12-01' = {
  name: openAiSettings.name
  location: location
  sku: {
    name: openAiSettings.sku
  }
  kind: 'OpenAI'
  properties: {
    customSubDomainName: openAiSettings.name
    publicNetworkAccess: 'Enabled'
  }
}

resource openAiCompletionsModelDeployment 'Microsoft.CognitiveServices/accounts/deployments@2022-12-01' = {
  parent: openAiAccount
  name: openAiSettings.completionsModel.deployment.name
  properties: {
    model: {
      format: 'OpenAI'
      name: openAiSettings.completionsModel.name
      version: openAiSettings.completionsModel.version
    }
    scaleSettings: {
      scaleType: 'Standard'
    }
  }
}

resource openAiEmbeddingsModelDeployment 'Microsoft.CognitiveServices/accounts/deployments@2022-12-01' = {
  parent: openAiAccount
  name: openAiSettings.embeddingsModel.deployment.name
  properties: {
    model: {
      format: 'OpenAI'
      name: openAiSettings.embeddingsModel.name
      version: openAiSettings.embeddingsModel.version
    }
    scaleSettings: {
      scaleType: 'Standard'
    }
  }
}

resource appServicePlan 'Microsoft.Web/serverfarms@2022-03-01' = {
  name: appServiceSettings.plan.name
  location: location
  sku: {
    name: appServiceSettings.plan.sku
  }
}

resource appServiceWeb 'Microsoft.Web/sites@2022-03-01' = {
  name: appServiceSettings.web.name
  location: location
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
  }
}

resource storageAccount 'Microsoft.Storage/storageAccounts@2021-09-01' = {
  name: '${name}fnstorage'
  location: location
  kind: 'Storage'
  sku: {
    name: 'Standard_LRS'
  }
}

resource appServiceFunction 'Microsoft.Web/sites@2022-03-01' = {
  name: appServiceSettings.function.name
  location: location
  kind: 'functionapp'
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      alwaysOn: true
    }
  }
  dependsOn: [
    storageAccount
  ]
}

resource appServiceWebSettings 'Microsoft.Web/sites/config@2022-03-01' = {
  parent: appServiceWeb
  name: 'appsettings'
  kind: 'string'
  properties: {
    APPINSIGHTS_INSTRUMENTATIONKEY: appServiceWebInsights.properties.InstrumentationKey
    COSMOSDB__ENDPOINT: cosmosDbAccount.properties.documentEndpoint
    COSMOSDB__KEY: cosmosDbAccount.listKeys().primaryMasterKey
    COSMOSDB__DATABASE: cosmosDatabase.name
    COSMOSDB__CONTAINERS: 'completions,product,customer'
    OPENAI__ENDPOINT: openAiAccount.properties.endpoint
    OPENAI__KEY: openAiAccount.listKeys().key1
    OPENAI__EMBEDDINGSDEPLOYMENT: openAiEmbeddingsModelDeployment.name
    OPENAI__COMPLETIONSDEPLOYMENT: openAiCompletionsModelDeployment.name
    OPENAI__MAXCONVERSATIONBYTES: openAiSettings.maxConversationBytes
    REDIS__CONNECTION: '${redisEnterprise.properties.hostName}:10000,abortConnect=false,ssl=true,password=${redisEnterpriseDatabase.listKeys().primaryKey}'
  }
}

resource appServiceFunctionSettings 'Microsoft.Web/sites/config@2022-03-01' = {
  parent: appServiceFunction
  name: 'appsettings'
  kind: 'string'
  properties: {
    AzureWebJobsStorage: 'DefaultEndpointsProtocol=https;AccountName=${name}fnstorage;EndpointSuffix=core.windows.net;AccountKey=${storageAccount.listKeys().keys[0].value}'
    APPINSIGHTS_INSTRUMENTATIONKEY: appServiceFunctionsInsights.properties.ConnectionString
    FUNCTIONS_EXTENSION_VERSION: '~4'
    FUNCTIONS_WORKER_RUNTIME: 'dotnet'
    CosmosDBConnection: cosmosDbAccount.listConnectionStrings().connectionStrings[0].connectionString
    OpenAiEndpoint: openAiAccount.properties.endpoint
    OpenAiKey: openAiAccount.listKeys().key1
    EmbeddingsDeployment: openAiEmbeddingsModelDeployment.name
    OpenAiMaxTokens: '8191'
    RedisConnection: '${redisEnterprise.properties.hostName}:10000,abortConnect=false,ssl=true,password=${redisEnterpriseDatabase.listKeys().primaryKey}'
  }
}

resource appServiceWebDeployment 'Microsoft.Web/sites/sourcecontrols@2021-03-01' = {
  parent: appServiceWeb
  name: 'web'
  properties: {
    repoUrl: appServiceSettings.web.git.repo
    branch: appServiceSettings.web.git.branch
    isManualIntegration: true
  }
  dependsOn: [
    appServiceWebSettings
  ]
}

resource appServiceFunctionsDeployment 'Microsoft.Web/sites/sourcecontrols@2021-03-01' = {
  parent: appServiceFunction
  name: 'web'
  properties: {
    repoUrl: appServiceSettings.web.git.repo
    branch: appServiceSettings.web.git.branch
    isManualIntegration: true
  }
  dependsOn: [
    appServiceFunctionSettings
  ]
}

resource appServiceFunctionsInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appServiceFunction.name
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
  }
}

resource appServiceWebInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appServiceWeb.name
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
  }
}

output deployedUrl string = appServiceWeb.properties.defaultHostName
