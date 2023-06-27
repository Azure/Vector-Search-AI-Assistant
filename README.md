# Vector Search & AI Assistant for Azure Cosmos DB for MongoDB vCore

This solution is a series of samples that demonstrate how to build solutions that incorporate Azure Cosmos DB with Azure OpenAI to build vector search solutions with an AI assistant user interface. The solution shows hows to generate vectors on data stored in Azure Cosmos DB using Azure OpenAI, then shows how to implment vector search capabilities using a variety of different vector capable databases available from Azure Cosmos DB and Azure.

The scenario for this sample centers around a consumer retail "Intelligent Agent" that allows users to ask questions on vectorized product, customer and sales order data stored in the database. The data in this solution is the [Cosmic Works](https://github.com/azurecosmosdb/cosmicworks) sample for Azure Cosmos DB. This data is an adapted subset of the Adventure Works 2017 dataset for a retail Bike Shop that sells bicycles, biking accessories, components and clothing.

## Solution Architecture

The solution architecture is represented by this diagram:

<p align="center">
    <img src="img/architecture.png" width="100%">
</p>

The application frontend is a Blazor application with Intelligent Agent UI functionality:

<p align="center">
    <img src="img/ui.png" width="100%">
</p>

## Getting Started

### Prerequisites

- Azure Subscription
- Subscription access to Azure OpenAI service. Start here to [Request Access to Azure OpenAI Service](https://customervoice.microsoft.com/Pages/ResponsePage.aspx?id=v4j5cvGGr0GRqy180BHbR7en2Ais5pxKtso_Pz4b1_xUOFA5Qk1UWDRBMjg0WFhPMkIzTzhKQ1dWNyQlQCN0PWcu)

### Deployment

Run the following script to provision the infrastructure and deploy the API and frontend

```pwsh
./scripts/Unified-Deploy.ps1 -resourceGroup <resource-group-name> -location <location> -subscription <subscription-id>
```

### Enabling/Disabling Deployment Steps

The following flags can be used to enable/disable specific deployment steps in the `Unified-Deploy.ps1` script.

| Parameter Name | Description |
|----------------|-------------|
| stepDeployArm | Enables or disables the provisioning of resources in Azure via ARM templates (located in `./arm`). Valid values are 0 (Disabled) and 1 (Enabled). See the `scripts/Deploy-Arm-Azure.ps1` script.
| stepBuildPush | Enables or disables the build and push of Docker images to the Azure Container Registry in the target resource group. Valid values are 0 (Disabled) and 1 (Enabled). See the `scripts/BuildPush.ps1` script.
| stepDeployCertManager | Enables or disables the Helm deployment of a LetsEncrypt capable certificate manager to the AKS cluster. Valid values are 0 (Disabled) and 1 (Enabled). See the `scripts/DeployCertManager.ps1` script.
| stepDeployTls | Enables or disables the Helm deployment of the LetsEncrypt certificate request resources to the AKS cluster. Valid values are 0 (Disabled) and 1 (Enabled). See the `scripts/PublishTlsSupport.ps1` script.
| stepDeployImages | Enables or disables the Helm deployment of the ChatServiceWebApi and Search services to the AKS cluster. Valid values are 0 (Disabled) and 1 (Enabled). See the `scripts/Deploy-Images-Aks.ps1` script.
| stepUploadSystemPrompts | Enables or disables the upload of OpenAI system prompt artifacts to a storage account in the target resource group. Valid values are 0 (Disabled) and 1 (Enabled). See the `scripts/UploadSystemPrompts.ps1` script.
| stepImportData | Enables or disables the import of data into a Cosmos account in the target resource group using the Data Migration Tool. Valid values are 0 (Disabled) and 1 (Enabled). See the `scripts/Import-Data.ps1` script.
| stepLoginAzure | Enables or disables interactive Azure login. If disabled, the deployment assumes that the current Azure CLI session is valid. Valid values are 0 (Disabled). 

Example command:
```pwsh
cd deploy/powershell
./Unified-Deploy.ps1 -resourceGroup myRg `
                     -subscription 0000... `
                     -stepLoginAzure 0 `
                     -stepDeployArm 0 `
                     -stepBuildPush 1 `
                     -stepDeployCertManager 0 `
                     -stepDeployTls 0 `
                     -stepDeployImages 1 `
                     -stepUploadSystemPrompts 0 `
                     -stepImportData 0
```

### Quickstart

1. After deployment is complete, go to the resource group for your deployment and open the Azure App Service in the Azure Portal. Click the link to launch the website.
1. Navigate to resource group and obtain the name of the AKS service and execute the following command to obtain the OpenAI Chat endpoint

  ```pwsh
  az aks show -n <aks-name> -g <resource-group-name> -o tsv --query addonProfiles.httpApplicationRouting.config.HTTPApplicationRoutingZoneName
  ```

1. Browse to the web app with the returned hostname.
1. Click [+ Create New Chat] button to create a new chat session.
1. Type in your questions in the text box and press Enter.

Here are some sample questions you can ask:

- What kind of socks do you have available?
- Do you have any customers from Canada? Where in Canada are they from?
- What kinds of bikes are in your product inventory?

### Real-time add and remove data

One great reason about using an operational database like Azure Cosmos DB as your source for data to be vectorized and search is that you can leverage its
Change Feed capability to dynamically add and remove products to the vector data which is searched. The steps below can demonstrate this capability.

#### Steps to demo adding and removing data from vector search

1. Start a new Chat Session in the web application.
1. In the chat text box, type: "Can you list all of your socks?". The AI Assistant will list 4 different socks of 2 types, racing and mountain.
1. Open a new browser tab, in the address bar type in `{your-app-name}-function.azurewebsites.net/api/addremovedata?action=add` replace the text in brackets with your application name, then press enter.
1. The browser should show that the HTTP Trigger executed successfully.
1. Return to the AI Assistant and type, ""Can you list all of your socks again?". This time you should see a new product, "Cosmic Socks, M"
1. Return to the second browser tab and type in, `{your-app-name}-function.azurewebsites.net/api/addremovedata?action=remove` replace the text in brackets with your application name, then press enter.
1. Open a **new** chat session and ask the same question again. This time it should show the original list of socks in the product catalog. 

**Note:** Using the same chat session after adding them will sometimes result in the Cosmic Socks not being returned. If that happens, start a new chat session and ask the same question. Also, sometimes after removing the socks they will continue to be returned by the AI Assistant. If that occurs, also start a new chat session. The reason this occurs is that previous prompts and completions are sent to OpenAI to allow it to maintain conversational context. Because of this, it will sometimes use previous completions as data to make future ones.

<p align="center">
    <img src="img/socks.png" width="100%">
</p>

## Clean-up

Delete the resource group to delete all deployed resources.


## Run locally and debug

This solution can be run locally post deployment. Below are the prerequisites and steps.

### Prerequisites for running/debugging locally

- Visual Studio, VS Code, or some editor if you want to edit or view the source for this sample.
- .NET 6 and 7 SDK
- Azure Functions SDK v4
- Azurite, for debugging using Azure Functions local storage.

### Local steps

#### Search Azure App Service
- Open the Configuration for the Azure App Service and copy the application setting values.
- Within Visual Studio, right click the Search project, then copy the contents of appsettings.json into the User Secrets. 
- If not using Visual Studio, create an `appsettings.Development.json` file and copy the appsettings.json and values into it.
 

#### Vectorize Azure Function
- Open the Configuration for the Azure Function copy the application setting values.
- Within Visual Studio, right click the Vectorize project, then copy the contents of the configuration values into User Secrets or local.settings.json if not using Visual Studio.


## Resources

- [Upcoming blog post announcement](https://devblogs.microsoft.com/cosmosdb/)
- [Azure Cosmos DB Free Trial](https://aka.ms/TryCosmos)
- [OpenAI Platform documentation](https://platform.openai.com/docs/introduction/overview)
- [Azure OpenAI Service documentation](https://learn.microsoft.com/azure/cognitive-services/openai/)