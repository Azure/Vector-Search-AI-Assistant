# Vector Search & AI Assistant for Azure Cosmos DB for MongoDB vCore v2

This solution demonstrates how to design and implement a **RAG Pattern** solution that incorporates Azure Cosmos DB for MongoDB vCore vector database capabilities with Azure OpenAI Service to build a vector search solution with an AI assistant user interface. The solution shows hows to generate vectors on data stored in Azure Cosmos DB for MongoDB vCore using Azure OpenAI Service, then shows how to generate vectors from natural language user input which is then used in to perform a vector search using Azure Cosmos DB for MongoDB vCore. The vector query response payload is then passed to Azure OpenAI Service which generates reponses back to the user. This solution includes additional key concepts such as managing conversational context and history, managing tokens consumed by Azure OpenAI Service, as well as understanding how to write prompts for large language models such as ChatGPT so they produce the desired responses.

The scenario for this sample centers around a consumer retail "Intelligent Agent" that allows users to ask questions on vectorized product, customer and sales order data stored in the database. The data in this solution is the [Cosmic Works](https://github.com/azurecosmosdb/cosmicworks) sample for Azure Cosmos DB. This data is an adapted subset of the Adventure Works 2017 dataset for a retail Bike Shop that sells bicycles, biking accessories, components and clothing.

## What is RAG?

RAG is an aconymn for Retrival Augmentmented Generation, a fancy term that essentially means retrieving additional data to provide to a large language model to use when generating a response (completion) to a user's natual language question (prompt). The data used in this type of application can be of any kind. However, there is a limit to how much data can be sent due to the limit of [tokens for each model](https://platform.openai.com/docs/models/overview) that can be consumed in a single request/response from Azure OpenAI Service. This solution will highlight these challenges and provide a simple example of how you can approach it.


## Solution User Experience

The application frontend is an ASP.NET Core Blazor Server application with Intelligent Agent UI functionality:

The application includes a left-hand nav that contains individual chat sessions. In a normal retail environment, users would only be able to see their own session but we've included them all here. The chat session includes a count of all of the tokens consumed in each session. When the user types a question and hits enter the service vectorizes the user input then uses that in a vector query against the data stored in Azure Cosmos DB for MongoDB vCore. The results of the query are then passed to Azure OpenAI Service which, along with the system prompts and conversation history, generates a response which is returned to the user. After the first question and response, the application calls a second "agent" which sends a request to the Azure OpenAI Service to summarize the topic of the conversation into a couple of words, then rename the chat session seen in the left-hand navigation. The chat session displays all of the tokens consumed for the length of that conversation. Each message in the chat also includes a token count. The `Prompt Tokens` are the tokens used in the call to Azure OpenAI Service along with the conversation history and vector query payload. The Assistant tokens are the ones used to generate the completion text.

<p align="center">
    <img src="img/ui.png" width="100%">
</p>

## Solution Architecture

The solution architecture is represented by this diagram:
This solution is composed of the following services:

1.  Azure Cosmos DB for MongoDB vCore - Stores the operational retail data, chat prompts and completions as well as the vectorized retail data for search.
1.	Azure Functions - Hosts two HTTP triggers, `Ingest And Vectorize` imports and vectorizes data. A second HTTP trigger, `Add Remove Data` is used to add and remove a product from the product catalog.
1.	Azure App Service - Hosts the Intelligent Agent web application.
1.	Azure OpenAI Service - Generates vectors using the Embeddings API on the `text-embedding-ada-002` model and chat completions using the Completion API on the `gpt-3.5-turbo` model.


<p align="center">
    <img src="img/architecture.png" width="100%">
</p>

## Overall solution workflow

There are four key elements of this solution, generating vectors, searching vectors, generating completions and storing conversations. Vectors are generated when data is inserted into Azure Cosmos DB for MongoDB vCore, then stored in a single collection that is used for vector searches. Users ask natural language questions using the web-based chat user interface (User Prompts). These prompts are then vectorized and used to perform a vector search of the vectorized data. The results are sent, along with some of the conversation history, to Azure OpenAI Service to generate a response (Completion) back to the user. All of the User Prompts and Completions are stored in a collection along with the number of tokens consumed by each Prompt and Completion. A Chat Session, contains all of the prompts and completions and a running total of all tokens consumed for that session.

## Generating vectors

Vectors are generated in two HTTP Triggers hosted in Azure Functions contained in the Vectorize project, [Ingest And Vectorize](https://github.com/Azure/Vector-Search-AI-Assistant/blob/mongovcorev2/Vectorize/IngestAndVectorize.cs) and [Add Remove Data](https://github.com/Azure/Vector-Search-AI-Assistant/blob/mongovcorev2/Vectorize/AddRemoveData.cs). `Ingest And Vectorize` imports the product, customer and sales order data from Azure Storage into collections, then generates vectors on all of the data and stores it in another collection that is configured with a vector index. The second HTTP trigger, `Add Remove Data` is used to add and vectorize a new product to the product catalog, making it available for searching in near real-time. The same trigger can be called again to remove that data to better simulate a real-world example.

You can see this at work by debugging Azure Functions remotely or running locally by setting a break point on [GenerateAndStoreVectors() function](https://github.com/Azure/Vector-Search-AI-Assistant/blob/mongovcorev2/Vectorize/IngestAndVectorize.cs#L104).

## Searching vectors

The web-based front-end provides users the means for searching the retail bike data for this solution using vector queries. Vector queries are performed in the [MongoDbService](https://github.com/Azure/Vector-Search-AI-Assistant/blob/mongovcorev2/Search/Services/MongoDbService.cs) in the Search project. In the chat UX a user starts a new chat session then types in a natural language question. The text is sent to Azure OpenAI Service's embeddings API to generate vectors on it. The vectors are then used to perform a vector search on the vectors collection in Azure Cosmos DB for MongoDB vCore. The query results are sent to Azure OpenAI Service, along with some of the conversation history and latest user qyestion to generate a completion or response, which is then passed back to the user.

## Key concepts this solution highlights

Building a solution like this introduces a number of concepts that may be new to many developers looking to build these types of applications. This solution was developed to make it easy to follow the execution flow for a RAG Pattern application. It does this from a single function in the Chat Service called [GetChatCompletionAsync()](https://github.com/Azure/Vector-Search-AI-Assistant/blob/mongovcorev2/Search/Services/ChatService.cs#L116). To best see this in action, set a breakpoint at the start of this function and step through each of the subsequent functions called to see each of the steps in action.

### Managing conversational context and history

Large language models such as Chat GPT do not by themselves keep any history of what prompts your've sent it, or what completions it has generated. It is up to the developer to do this. Keeping this history is necessary or helpful for a few reasons. First, it allows users to ask follow up questions without having to provide any subsequent context, allowing for the user to have a conversation with the Intelligent Agent. Second, the conversation context is useful when performing vector searches as it can provide additional detail on what the user is looking for. As an example, if I asked our Intelligent Retail Agent "what bikes are available?", it would return for me all of the bikes in stock. If I then asked, "what colors are there?", if I did not pass the first prompt and completion, the vector search would not know that the user was asking about bike colors and would likely not produce an accurate or meaningful response. Lastly, the storing the chat history can be useful auditing purposes for customer interactions. Especially when trying to further improve the performance and quality of the vector search results and generated completions by Azure OpenAI.

The function that manages conversational history is called, [GetChatSessionConversation()](https://github.com/Azure/Vector-Search-AI-Assistant/blob/mongovcorev2/Search/Services/ChatService.cs#L221). This function is used to gather the most convesation history up to the `Max Conversation Tokens` limit, then returns it as a string separating each prompt and completion with a new line character. The new line is not necessary for ChatGPT, but makes it more readible for a user when debugging. This function also returns the number of tokens used in the conversation. This value is used when building the prompt to send.

### Vectorizing the user prompt and conversation history

In a vector search solution, the filter predicate for any query is an array of vectors. This means that the text the user types in to the chat window, plus any conversational context that is gathered, must first be vectorized before the vector search can be done. This is accomplished in the [OpenAiService](https://github.com/AzureCosmosDB/VectorSearchAiAssistant/blob/mongovcorev2/Search/Services/OpenAiService.cs) in the solution in the [GetEmbeddingsAsync()](https://github.com/AzureCosmosDB/VectorSearchAiAssistant/blob/mongovcorev2/Search/Services/OpenAiService.cs#L113) function. This function takes a string and returns an array of vectors, along with the number of tokens used by the service.

### Doing the vector search

The vector search is the key function in this solution and is done against the Azure Cosmos DB for MongoDB vCore database in this solution. The function itself is rather simple and only takes and array of vectors with which to do the search. You can see the vector search at work by debugging the Azure Web App remotely or running locally. Set a break point on [VectorSearchAsync()](https://github.com/AzureCosmosDB/VectorSearchAiAssistant/blob/mongovcorev2/Search/Services/MongoDbService.cs#L105), then step through each line to see how of the function calls to see the search and returned data.

### Token management

One of the more challenging aspects to building RAG Pattern solutions is manging the tokens to stay within the maximum number of tokens that can be consumed in a single request (prompt) and response (completion). It's possible to build a prompt that consumes all of the tokens in the requests and leaves too few to produce a useful response. It's also possible to generate an exception from the Azure OpenAI Service if the request itself is over the token limit. We need a way to measure token usage before sending the request. This is handled in the [BuildPromptAndData()](https://github.com/Azure/Vector-Search-AI-Assistant/blob/mongovcorev2/Search/Services/ChatService.cs#L169) function in the Chat Service. This function uses a third party nuget package called, [SharpToken](https://github.com/dmitry-brazhenko/SharpToken) which is a .NET wrapper around [OpenAI's tiktoken](https://github.com/openai/openai-cookbook/blob/main/examples/How_to_count_tokens_with_tiktoken.ipynb) which is an open source tokenizer. The utility takes text and generates an array of vectors. The number of elements in the array represent the number of tokens that will be consumed. It can also do the reverse and take an array of vectors and output text. In our function here we first generate the vectors on the data returned from our vector search, then if necessary, reduce the amount of data by calculating the number of vectors we can safely pass in our request to Azure OpenAI Service. Here is the flow of this function.

1. Measure the amount of tokens on the vector search results (rag data).
1. Measure the amount of tokens for the user prompt. This data is also used to capture what the user prompt tokens would be if processed without any additional data and stored in the user prompt message in the completions collection (more on that later).
1. Calculate if the amount of tokens used by the `search results` plus the `user prompt` plus the `conversation` + `completion` is greater than what the model will accept. If it is greater, then calculate how much to reduce the amount of data and `Decode` the vector array we generated from the search results, back into text.
1. Finally, return the text from our search results as well as the number of tokens for the last User Prompt (this will get stored a bit later).

### Generate the completion

Generating a chat completion from Azure OpenAI Service is done using one of its [GPT models](https://platform.openai.com/docs/guides/gpt). The Azure OpenAI Service takes in all of the data we've gathered up to this point, then generates a response or completion which the user will see. All of this happens in the [OpenAiService](https://github.com/Azure/Vector-Search-AI-Assistant/blob/mongovcorev2/Search/Services/OpenAiService.cs) in the [GetChatCompletionAsync()](https://github.com/Azure/Vector-Search-AI-Assistant/blob/mongovcorev2/Search/Services/OpenAiService.cs#L155) function. 

This function takes the user prompt and the search results and builds a `System Prompt` with the search data, as well as a user prompt that includes the conversation history plus the users last question (prompt). The call is then made to the service which returns a `ChatCompletions` object which contains the response text itself, plus the number of tokens used in the request (prompt) and the number of tokens used to generate the response (completion). 

One thing to note here is it is necessary to separate the number of tokens from the Prompt with the data versus the number of tokens from the text the user typed into the chat interface. This is necessary because we need an accurate way to estimate the number of tokens for *just the text* of the user prompt and not from the data.

### Saving the results

The last part is to save the results of both our user prompt and completion as well as the amount of tokens used. All of the conversational history and the amount of tokens used in each prompt and completion is stored in the completions collection in the Azure Cosmos DB for MongoDB vCore database in this solution. The call to the MongoDB vCore service is made by another function within our ChatService called, [AddPromptCompletionMessagesAsync()](https://github.com/Azure/Vector-Search-AI-Assistant/blob/mongovcorev2/Search/Services/ChatService.cs#L290). This function creates two new [Message](https://github.com/Azure/Vector-Search-AI-Assistant/blob/mongovcorev2/Search/Models/Message.cs) objects and stores them in a local cache of all the Sessions and Messages for the application. It then adds up all of the tokens used and saves it to the Session object which keeps a running total for the entire [Session](https://github.com/Azure/Vector-Search-AI-Assistant/blob/mongovcorev2/Search/Models/Session.cs).

The data is then saved in the [UpdateSessionBatchAsync()](https://github.com/Azure/Vector-Search-AI-Assistant/blob/mongovcorev2/Search/Services/MongoDbService.cs#L257) function in the MongoDbService. This function creates a new transaction then updates the Session document and inserts two new Message documents into the completions collection.



## Getting Started

### Prerequisites

- Azure Subscription
- Subscription access to Azure OpenAI Service. Start here to [Request Access to Azure OpenAI Service](https://customervoice.microsoft.com/Pages/ResponsePage.aspx?id=v4j5cvGGr0GRqy180BHbR7en2Ais5pxKtso_Pz4b1_xUOFA5Qk1UWDRBMjg0WFhPMkIzTzhKQ1dWNyQlQCN0PWcu)

### Installation

1. Fork this repository to your own GitHub account.
1. Depending on whether you deploy using the ARM Template or Bicep, modify "appGitRepository" variable in one of those files to point to your fork of this repository: https://github.com/Azure/Vector-Search-AI-Assistant.git (Note that this solution is in the mongodbvcorev2 branch)
1. If using the Deploy to Azure button below, also modify this README.md file to change the path for the Deploy To Azure button to your local repository.
1. If you deploy this application without making either of these changes, you can update the repository by disconnecting and connecting an external git repository pointing to your fork.


The provided ARM or Bicep Template will provision the following resources:
1. Azure Cosmos DB for MongoDB vCore. This stores retail data, vectors and the user prompts and completions from the chat application.
1. Azure App service. This will be configured to deploy the Search web application from **this** GitHub repository. This will work fine if no changes are made. If you want it to deploy from your forked repository, modify the Deploy To Azure button below.
1. Azure Open AI account with the `gpt-35-turbo` and `text-embedding-ada-002` models deployed.
1. Azure Functions. This will run on the same hosting plan as the Azure App Service.

**Note:** You must have access to Azure OpenAI Service from your subscription before attempting to deploy this application.

All connection information for Azure Cosmos DB and Azure OpenAI Service is zero-touch and injected as environment variables into Azure App Service and Azure Functions at deployment time. 

[![Deploy to Azure](https://aka.ms/deploytoazurebutton)](https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2FAzure%2FVector-Search-Ai-Assistant%2Fmongovcorev2%2Fazuredeploy.json)

### Initial data load

The data for this solution must be loaded and vectorized once it has been deployed. This process takes approximately 5-10 minutes to complete. Follow the steps below.

1. Open a browser so you can watch Azure Functions processing the data by navigating to each of the Azure Functions in the portal by navigating to the Log stream tab in the Monitoring section on the left hand side of the page. **Note:** you will need to enable Application Insights for the Azure Functions in the portal when first accessing the Functions Logs. 
1. To start the data load and vector generation, open a new browser tab, in the address bar type in `{your-app-name}-function.azurewebsites.net/api/ingestandvectorize`


<p align="center">
    <img src="img/dataloadandvectors.png" width="100%">
</p>

### Running the solution

1. After data loading is complete, you can open the Chat Web Application and run the app.
1. Open a new browser and type in the address bar `{your-app-name}-web.azurewebsites.net` You can also open the Azure Portal, go to the resource group for your deployment, open the Azure App Service. Then on the Overview tab, click the link to launch the website.
1. Once the web application loads, click [+ Create New Chat] button to create a new chat session.
1. Type in your questions in the text box and press Enter.

Here are some sample questions you can ask:

- What kind of socks do you have available?
- Do you have any customers from Canada? Where in Canada are they from?
- What kinds of bikes are in your product inventory?

### Real-time add and remove data

One great reason about using an operational database like Azure Cosmos DB for MongoDB vCore as your source for data to be vectorized and search is that you can dynamically add and remove data to be vectorized and immediately available for search. The steps below can demonstrate this capability.

#### Steps to demo adding and removing data from vector search

1. Start a new Chat Session in the web application.
1. In the chat text box, type: "Can you list all of your socks?". The AI Assistant will list 4 different socks of 2 types, racing and mountain.
1. Open a new browser tab, in the address bar type in `{your-app-name}-function.azurewebsites.net/api/addremovedata?action=add` replace the text in curly braces with your application name, then press enter.
1. The browser should show that the HTTP Trigger executed successfully.
1. Return to the AI Assistant and type, ""Can you list all of your socks again?". This time you should see a new product, "Cosmic Socks, M"
1. Return to the second browser tab and type in, `{your-app-name}-function.azurewebsites.net/api/addremovedata?action=remove` replace the text in curly braces with your application name, then press enter.
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
- .NET 7 SDK
- Azure Functions SDK v4
- Azurite, for debugging using Azure Functions local storage.

### Local steps

#### Search Azure App Service
- In Azure Portal, open the Configuration for the Azure App Service, click Show Values and keep open.
- Within Visual Studio, right click the Search project, create a new file, `appsettings.Development.json`.
- Copy the contents of the JSON below.
- Copy the missing connection information from the Configuration values in Azure Portal.

```json
{
  "DetailedErrors": true,
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "OpenAi": {
    "Endpoint": "",
    "Key": "",
    "EmbeddingsDeployment": "embeddings",
    "CompletionsDeployment": "completions",
    "MaxConversationTokens": "100",
    "MaxCompletionTokens": "500"
  },
  "MongoDb": {
    "Connection": "",
    "DatabaseName": "retaildb",
    "CollectionNames": "product,customer,vectors,completions",
    "MaxVectorSearchResults": "10"
  }
}
```
 

#### Vectorize Azure Function
- Open the Configuration for the Azure Function, click Show Values and keep open copy.
- Within Visual Studio, right click the Vectorize project, create a new file called, `local.settings.json`
- Copy the contents of the JSON below
- Copy the missing connection strings and keys from the Configuration values in Azure Portal.

```json
{
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "OpenAi__Endpoint": "",
    "OpenAi__Key": "",
    "OpenAi__EmbeddingsDeployment": "embeddings",
    "OpenAi__MaxTokens": "8191",
    "MongoDb__Connection": "",
    "MongoDb__DatabaseName": "retaildb",
    "MongoDb__CollectionNames": "product,customer,vectors,completions"
  },
  "IsEncrypted": false
}
```

### Run it!
If running in Visual Studio, configure both the Search and Vectorize projects to start. Place breakpoints where you'd like to step through the source. Press F5 to start them in a debugger.


That's it!!! I hope you enjoy this solution

## Resources

- [Azure Cosmos DB Free Trial](https://aka.ms/TryCosmos)
- [OpenAI Platform documentation](https://platform.openai.com/docs/introduction/overview)
- [Azure OpenAI Service documentation](https://learn.microsoft.com/azure/cognitive-services/openai/)
