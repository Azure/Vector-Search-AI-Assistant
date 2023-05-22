# Vector Search & AI Assistant for Azure Cosmos DB 

This solution is a series of samples that demonstrate how to build solutions that incorporate Azure Cosmos DB with Azure OpenAI to build vector search solutions with an AI assistant user interface. The solution shows hows to generate vectors on data stored in Azure Cosmos DB using Azure OpenAI, then shows how to implment vector search capabilities using a variety of different vector capable databases available from Azure Cosmos DB and Azure.

The scenario for this sample centers around a consumer retail "Intelligent Agent" that allows users to ask questions on vectorized product, customer and sales order data stored in the database. The data in this solution is the [Cosmic Works](https://github.com/azurecosmosdb/cosmicworks) sample for Azure Cosmos DB. This data is an adapted subset of the Adventure Works 2017 dataset for a retail Bike Shop that sells bicycles, biking accessories, components and clothing.

This repository has multiple versions of this solution which can be downloaded via One-Click Azure Deploy and used:

For the Azure Cosmos DB for MongoDB vCore version of this solution see, https://github.com/AzureCosmosDB/VectorSearchAiAssistant/tree/MongovCore


For the Redis version of this solution see, https://github.com/AzureCosmosDB/VectorSearchAiAssistant/tree/Redis

More versions are Coming Soon!!!


## Solution Architecture

The solution architecture is represented by this diagram:

<p align="center">
    <img src="img/architecture.png" width="100%">
</p>

The application frontend is a Blazor application with basic Q&A functionality:

<p align="center">
    <img src="img/ui.png" width="100%">
</p>



## Resources

- [Upcoming blog post announcement](https://devblogs.microsoft.com/cosmosdb/)
- [Azure Cosmos DB Free Trial](https://aka.ms/TryCosmos)
- [OpenAI Platform documentation](https://platform.openai.com/docs/introduction/overview)
- [Azure OpenAI Service documentation](https://learn.microsoft.com/azure/cognitive-services/openai/)