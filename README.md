# Vector Search & AI Assistant for Azure Cosmos DB for MongoDB vCore

This solution is a series of samples that demonstrate how to build solutions that incorporate Azure Cosmos DB with Azure OpenAI to build vector search solutions with an AI assistant user interface. The solution shows hows to generate vectors on data stored in Azure Cosmos DB using Azure OpenAI, then shows how to implment vector search capabilities using Azure Cosmos DB for MongoDB vCore capable. The user experience is a Retail AI Assistant. The data in this scenario centers around answering specific questions about the data stored in Azure Cosmos DB in a consumer retail "Intelligent Agent" workload that allows users to ask questions on products and customer stored in the database. The data for the prompts and completions are also stored in Azure Cosmos DB along with the token usage. This data can further be developed to allow users to develop more refine prompts for their own data. 

The data used in this solution is from the [Cosmic Works](https://github.com/azurecosmosdb/cosmicworks) sample for Azure Cosmos DB, adapted from the Adventure Works dataset for a retail Bike Shop that sells bicycles as well as biking accessories, components and clothing.

## Solution Architecture

The solution architecture is represented by this diagram:

<p align="center">
    <img src="img/architecture.png" width="100%">
</p>

The application frontend is a Blazor application with Intelligent Agent UI functionality:

<p align="center">
    <img src="img/ui.png" width="100%">
</p>

This solution is composed of the following services:

1.	Azure Cosmos DB - Stores the operational retail data, generated embeddings and chat prompts and completions.
1.  Azure Cosmos DB for MongoDB vCore - stores the vectorized retail data for search.
1.	Azure Functions - Hosts a Cosmos DB trigger to generate embeddings, Cosmos DB output binding to save the embeddings and Azure Cosmos DB for MongoDB vCore.
1.	Azure OpenAI - Generates embeddings using the Embeddings API and chat completions using the Completion API.
1.	Azure App Service - Hosts Intelligent Agent UI.

## Getting Started

### Prerequisites

- Azure Subscription
- Subscription access to Azure OpenAI service. Start here to [Request Access to Azure OpenAI Service](https://customervoice.microsoft.com/Pages/ResponsePage.aspx?id=v4j5cvGGr0GRqy180BHbR7en2Ais5pxKtso_Pz4b1_xUOFA5Qk1UWDRBMjg0WFhPMkIzTzhKQ1dWNyQlQCN0PWcu)

### Installation

1. Fork this repository to your own GitHub account.
1. Depending on whether you deploy using the ARM Template or Bicep, modify "appGitRepository" variable in one of those files to point to your fork of this repository: https://github.com/AzureCosmosDB/VectorSearchAiAssistant.git (Be sure to have the right branch that corresponds with the vector search database you are using)
1. If using the Deploy to Azure button below, also modify this README.md file to change the path for the Deploy To Azure button to your local repository.
1. If you deploy this application without making either of these changes, you can update the repository by disconnecting and connecting an external git repository pointing to your fork.


The provided ARM or Bicep Template will provision the following resources:
1. Azure Cosmos DB for NoSQL account with a database and 5 containers at 1000 RU/s autoscale.
1. Azure Cosmos DB for MongoDB vCore for vector search.
1. Azure App service. This will be configured to deploy the Search web application from **this** GitHub repository. This will work fine if no changes are made. If you want it to deploy from your forked repository, modify the Deploy To Azure button below.
1. Azure Open AI account with the `gpt-35-turbo` and `text-embedding-ada-002` models deployed.
1. Azure Functions. This will run on the same hosting plan as the Azure App Service.

**Note:** You must have access to Azure OpenAI service from your subscription before attempting to deploy this application.

All connection information for Azure Cosmos DB and Azure OpenAI is zero-touch and injected as environment variables into Azure App Service and Azure Functions at deployment time. 

[![Deploy to Azure](https://aka.ms/deploytoazurebutton)](https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2FAzureCosmosDB%2FVectorSearchAiAssistant%2FMongovCore%2Fazuredeploy.json)

### Initial data load

The data for this solution must be loaded once it has been deployed. This process takes approximately 10 minutes to complete. The process for loading data also starts the process of generating vectors for all of the operational retail data in this solution. Follow the steps below.

1. Download and install the [Azure Cosmos DB Data Migration Desktop Tool](https://github.com/AzureCosmosDB/data-migration-desktop-tool/releases)
1. Copy the `migrationsettings.json` from the root folder of this repository and replace the version in the folder where you downloaded the tool above.
1. Open the file using any text editor.
1. Open the Azure Cosmos DB blade in the resource group for this solution.
1. Navigate to the Keys blade in Azure Portal and copy the Primary Connection String for the Azure Cosmos DB for NoSQL account.
1. Paste the connection string to replace to placeholders called `ADD-COSMOS-CONNECTION-STRING`. Save the file.
1. Run dmt.exe
1. You can watch Azure Functions processing the data by navigating to each of the Azure Functions in the portal.

<p align="center">
    <img src="img/monitorfunctions.png" width="100%">
</p>

### Quickstart

1. After data loading is complete, go to the resource group for your deployment and open the Azure App Service in the Azure Portal. Click the link to launch the website.
1. Click [+ Create New Chat] button to create a new chat session.
1. Type in your questions in the text box and press Enter.

Here are some sample questions you can ask:

- What kind of socks do you have available?
- Do you have any customers from Canada? Where in Canada are they from?
- What kinds of bikes are in your product inventory?


## Clean-up

Delete the resource group to delete all deployed resources.


## Run locally and debug

This solution can be run locally post deployment. Below are the prerequisites and steps.

### Prerequisites for running/debugging locally

- Visual Studio, VS Code, or some editor if you want to edit or view the source for this sample.
- .NET 7 SDK
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