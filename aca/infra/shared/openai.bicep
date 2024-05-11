param deployments array
param location string = resourceGroup().location
param name string
param sku string = 'S0'
param tags object = {}


resource openAi 'Microsoft.CognitiveServices/accounts@2023-05-01' = {
  name: name
  location: location
  sku: {
    name: sku
  }
  kind: 'OpenAI'
  properties: {
    customSubDomainName: name
    publicNetworkAccess: 'Enabled'
  }
  tags: tags
}

@batchSize(1)
resource openAiDeployments 'Microsoft.CognitiveServices/accounts/deployments@2023-05-01' = [
  for deployment in deployments: {
    parent: openAi
    name: deployment.name
    sku: {
      capacity: deployment.sku.capacity
      name: deployment.sku.name
    }
    properties: {
      model: {
        format: 'OpenAI'
        name: deployment.model.name
        version: deployment.model.version
      }
    }
  }
]

output endpoint string = openAi.properties.endpoint
output name string = openAi.name
