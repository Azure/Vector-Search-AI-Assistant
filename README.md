# Vector Search & AI Assistant for Azure Cosmos DB 

This solution is a series of samples that demonstrate how to build solutions that incorporate Azure Cosmos DB with Azure OpenAI to build vector search solutions with an AI assistant user interface. The solution shows hows to generate vectors on data stored in Azure Cosmos DB using Azure OpenAI, then shows how to implment vector search capabilities using a variety of different vector capable databases available from Azure Cosmos DB and Azure. The solution also includes key concepts such as managing conversational context and history, managing tokens consumed by Azure OpenAI, as well as understanding how to write prompts for large language models such as ChatGPT so they produce the desired responses.

The scenario for this sample centers around a consumer retail "Intelligent Agent" that allows users to ask questions on vectorized product, customer and sales order data stored in the database. The data in this solution is the [Cosmic Works](https://github.com/azurecosmosdb/cosmicworks) sample for Azure Cosmos DB. This data is an adapted subset of the Adventure Works 2017 dataset for a retail Bike Shop that sells bicycles, biking accessories, components and clothing.

## What is RAG?

RAG is an aconymn for Retrival Augmentmented Generation, a fancy term that essentially means retrieving additional data to provide to a large language model to use when generating a response (completion) to a user's question(prompt). The data can be any kind of text. However, there is a limit to how much text can be sent due to the limit of [tokens for each model](https://platform.openai.com/docs/models/overview) that can be consumed in a single request/response from Azure OpenAI. This solution will highlight these challenges and other challenges faced when designing and building this type of solution and provide  examples of how we addressed them.

## Explore the different solutions
This repository has multiple versions of this solution which can be downloaded via One-Click Azure Deploy and used:

- Azure Cosmos DB for MongoDB vCore version, https://github.com/AzureCosmosDB/VectorSearchAiAssistant/tree/MongovCorev2
- Redis Enterprise version, https://github.com/AzureCosmosDB/VectorSearchAiAssistant/tree/Redis
- More versions are Coming Soon!!!


## Solution User Experience

The application frontend is a Blazor application with Intelligent Agent UI functionality:

The application includes a left-hand nav that contains individual chat sessions. In a normal retail environment, users would only be able to see their own session but we've included them all here. The chat session includes a count of all of the tokens consumed in each session. When the user types a question and hits enter the service queries the vector data, then sends the response to Azure OpenAI which then generates a completion which is the displayed to the user. The first question also triggers the chat session to be named with whatever the user is asking about. Users can rename a chat if they like or delete it. The chat session displays all of the tokens consumed for that session. Each message in the chat also includes a token count. The `Prompt Tokens` are the tokens used in the call to Azure OpenAI. The Assistant tokens are the ones used to generate the completion text.

<p align="center">
    <img src="img/ui.png" width="100%">
</p>

## Support

The purpose of these solutions is to provide users with examples of how to design and build this type of solution using the various services available in Azure. While these solutions are hosted within the Azure Cosmos DB GitHub organization, they are not officially supported. Users are welcome to submit issues, any fixes are done on a best effort bassi. Users are most welcome and to submit Pull Requests.


## Resources

- [Azure Cosmos DB Free Trial](https://aka.ms/TryCosmos)
- [OpenAI Platform documentation](https://platform.openai.com/docs/introduction/overview)
- [Azure OpenAI Service documentation](https://learn.microsoft.com/azure/cognitive-services/openai/)