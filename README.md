# Vector Search & AI Assistant for Azure Cosmos DB & Redis

This solution is a series of samples that demonstrate how to build solutions that incorporate Azure Cosmos DB with Azure OpenAI to build vector search solutions with an AI assistant user interface. The solution shows hows to generate vectors on data stored in Azure Cosmos DB using Azure OpenAI, then shows how to implment vector search capabilities using a variety of different vector capable databases available from Azure Cosmos DB and Azure.

The scenario for this sample centers around a consumer retail "Intelligent Agent" that allows users to ask questions on vectorized product, customer and sales order data stored in the database. The data in this solution is the [Cosmic Works](https://github.com/azurecosmosdb/cosmicworks) sample for Azure Cosmos DB. This data is an adapted subset of the Adventure Works 2017 dataset for a retail Bike Shop that sells bicycles, biking accessories, components and clothing.


## Solution Architecture

The solution architecture is represented by this diagram:

<p align="center">
    <img src="img/architecture.png" width="100%">
</p>

The application frontend is a Blazor application with a chat user experience:

<p align="center">
    <img src="img/ui.png" width="100%">
</p>

This solution is composed of the following services:

1.	Azure Cosmos DB - Stores the operational retail data, generated embeddings and chat prompts and completions.
1.	Azure Functions - Hosts a Cosmos DB trigger to generate embeddings, Cosmos DB output binding to save the embeddings and Redis.
1.	Azure OpenAI - Generates embeddings using the Embeddings API and chat completions using the Completion API.
1.	Azure Cache for Redis Enterprise - Performs vector matching.
1.	Azure App Service - Hosts Intelligent Agent UI.

## Overall solution workflow

There are two key elements of this solution, generating vectors and searching vectors. Vectors are generated when data is inserted into Azure Cosmos DB, then cached in Redis. Users can then ask questions using web-based chat user interface to search the cached vectorized data and return augmented data to Azure OpenAI to generate a completion back to the user.

### Generating vectors

Vectors are generated in two Azure Functions contained in the Vectorize project, [Products](https://github.com/AzureCosmosDB/VectorSearchAiAssistant/blob/Redis/Vectorize/Products.cs) and [CustomersAndOrders](https://github.com/AzureCosmosDB/VectorSearchAiAssistant/blob/Redis/Vectorize/CustomersAndOrders.cs). Vector generation starts with the data loading for this solution which loads data into Azure Cosmos DB from JSON files stored in Azure Storage. The containers in Cosmos have change feed running on them. When the data is inserted the Azure Function calls Azure OpenAI and passes the entire document to it, then returns vectorized data. This data is then saved to Redis as well as written to another container in Azure. If the Redis cache were to require reloading of the data, the Azure Function can re-cache the data from the vectorized data stored in Azure Cosmos DB.

You can see this at work by debugging Azure Functions remotely or running locally. You can set a break point on the [GenerateProductVectors() function](https://github.com/AzureCosmosDB/VectorSearchAiAssistant/blob/Redis/Vectorize/Products.cs#L61), [GenerateCustomerVectors() function](https://github.com/AzureCosmosDB/VectorSearchAiAssistant/blob/Redis/Vectorize/CustomersAndOrders.cs#L78) or [GenerateOrderVectors() function](https://github.com/AzureCosmosDB/VectorSearchAiAssistant/blob/Redis/Vectorize/CustomersAndOrders.cs#L107)

## Searching vectors

The web-based front-end for this solution provides users the means for searching the vectorized retail bike data for this solution. This work is centered around the [ChatService](https://github.com/AzureCosmosDB/VectorSearchAiAssistant/blob/Redis/Search/Services/ChatService.cs) in the Search project. In the chat UX a user starts a new chat session then types in a question. The text that is entered is sent to Azure OpenAI's embeddings API to generate vectors on the text. This is what occurs when they data is initially loaded. That vectorized text is then sent to Redis to perform a vector search on the cached data vector data. The response to this query provides  pointers for where the source data is stored in Azure Cosmos DB that includes the name of the container, the partition key value and the id value. This array of pointers is then sent to the Cosmos DB service where the data is fetched, then sent to Azure OpenAI to generate a completion which is then passed back to the user as a response.

You can see this at work by debugging the Azure Web App remotely or running locally. Set a break point on [GetChatCompletionAsync()](https://github.com/AzureCosmosDB/VectorSearchAiAssistant/blob/Redis/Search/Services/ChatService.cs#L115), then step through each of the function calls to see each step in action.


## Getting Started

### Prerequisites

- Azure Subscription
- Subscription access to Azure OpenAI service. Start here to [Request Access to Azure OpenAI Service](https://customervoice.microsoft.com/Pages/ResponsePage.aspx?id=v4j5cvGGr0GRqy180BHbR7en2Ais5pxKtso_Pz4b1_xUOFA5Qk1UWDRBMjg0WFhPMkIzTzhKQ1dWNyQlQCN0PWcu)

### Installation

1. Fork this repository to your own GitHub account.
1. Depending on whether you deploy using the ARM Template or Bicep, modify "appGitRepository" variable in one of those files to point to your fork of this repository: https://github.com/AzureCosmosDB/VectorSearchAiAssistant.git, You will also need to modify the branch for this as well to `Redis`
1. If using the Deploy to Azure button below, also modify this README.md file to change the path for the Deploy To Azure button to your local repository.
1. If you deploy this application without making either of these changes, you can update the repository by disconnecting and connecting an external git repository pointing to your fork.


The provided ARM or Bicep Template will provision the following resources:
1. Azure Cosmos DB account with a database and 5 containers at 1000 RU/s autoscale. These will scale down to 500 RU/s when not in use.
1. Azure App service. This will be configured to deploy the Search web application from **this** GitHub repository. This will work fine if no changes are made. If you want it to deploy from your forked repository, modify the Deploy To Azure button below.
1. Azure Open AI account with the `gpt-35-turbo` and `text-embedding-ada-002` models deployed.
1. Azure Functions. This will run on the same hosting plan as the Azure App Service.
1. Azure Cache for Redis Enterprise. **Please note that this service costs a minimum of $700 per month.**

**Note:** You must have access to Azure OpenAI service from your subscription before attempting to deploy this application.

All connection information for Azure Cosmos DB, Azure OpenAI and Azure Cache for Redis is zero-touch and injected as environment variables into Azure App Service and Azure Functions at deployment time. 

[![Deploy to Azure](https://aka.ms/deploytoazurebutton)](https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2FAzureCosmosDB%2FVectorSearchAiAssistant%2FRedis%2Fazuredeploy.json)

### Initial data load

The data for this solution must be loaded once it has been deployed. This process takes approximately 10 minutes to complete. The process for loading data also starts the process of generating vectors for all of the operational retail data in this solution. Follow the steps below.

1. Download and install the [Azure Cosmos DB Data Migration Desktop Tool](https://github.com/AzureCosmosDB/data-migration-desktop-tool/releases)
1. Copy the `migrationsettings.json` from the root folder of this repository and replace the version in the folder where you downloaded the tool above.
1. Open the file using any text editor.
1. Open the Azure Cosmos DB blade in the resource group for this solution.
1. Navigate to the Keys blade in Azure Portal and copy the Primary Connection String for the Cosmos DB account.
1. Paste the connection string to replace to placeholders called `ADD-COSMOS-CONNECTION-STRING`. Save the file.
1. Run .\dmt.exe from any shell.
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

### Real-time add and remove data

The best part about starting with an operational database like Azure Cosmos DB as your source for data to be vectorized and search is that you can leverage its
Change Feed capability to dynamically add and remove products to the vector data which is searched. This demo can demonstrate that capability as well.

#### Steps to demo adding and removing data from vector search

1. Start a new Chat Session in the web application.
1. In the chat text box, type: "Can you list all of your socks?". The AI Assistant will list 5 different types of socks.
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

To remove all the resources used by this sample, you must first manually delete the deployed model within the Azure OpenAI service. You can then delete the resource group for your deployment. This will delete all remaining resources.


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