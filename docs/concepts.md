## Key Concepts

There are a number of key concepts for building Generative AI within this this solution: generating vectors, searching vectors, generating completions, storing chat conversations and caching. Vectors are generated when data is inserted into Azure Cosmos DB, then stored in an Azure Cosmos DB vector index that is used for vector searches. Users then ask natural language questions using the web-based chat user interface (User Prompts). These prompts are then vectorized and used to search the vectorized data. The results are then sent, along with some of the conversation history, to Azure OpenAI Service to generate a response (Completion) back to the user. New completions are also stored in a semantic cache that is consulted for each new user prompt. All of the User Prompts and Completions are stored in a Cosmos DB container along with the number of tokens consumed by each Prompt and Completion. A Chat Session contains all of the prompts and completions and a running total of all tokens consumed for that session. In a production environment users would only be able to see their own sessions but this solution shows all sessions from all users.

## Generating vectors

Vectors are generated in Change Feed handler (GenericChangeFeedHandler() method) contained in the `Infrastructure` project which monitors the changes in `customer` and `products` containers. As soon as a new document is inserted into either of these containers, the Change Feed handler will generate a vector and add the document and its embedding to the `main-vector-store` Cosmos DB collection.


## Searching vectors

The web-based front-end provides users the means for searching the vectorized retail bike data for this solution. This work is centered around the [AzureCosmosDBNoSqlMemoryStore](https://github.com/Azure/BuildYourOwnCopilot/blob/main/src/SemanticKernel/Connectors/Connectors.Memory.AzureCosmosDBNoSql/AzureCosmosDBNoSqlMemoryStore.cs) in the `SemanticKernel` project. In the chat UI, a user starts a new chat session then types in a natural language question. The text is sent to Azure OpenAI Service embeddings API to generate vectors on it. The vectors are then used to perform a vector search on the vectors collection in Azure Cosmos DB. The query response which includes the original source data is sent to Azure OpenAI Service to generate a completion which is then passed back to the user as a response.


## Managing conversational context and history

Large language models such as ChatGPT do not keep any history of what prompts users sent it, or what completions it generated. It is up to the developer to do this. Keeping this history is necessary for two reasons. First, it allows users to ask follow-up questions without having to provide any context, while also allowing the user to have a conversation with the model. Second, the conversation history is useful when performing vector searche on data as it provides additional detail on what the user is looking for. As an example, if I asked our Intelligent Retail Agent what bikes it had available, it would return for me all of the bikes in stock. If I then asked, "what colors are available?", if I did not pass the first prompt and completion, the vector search would not know that the user was asking about bike colors and would likely not produce an accurate or meaningful response.

Another concept surfaced with conversation management centers around tokens. All calls to Azure OpenAI Service are limited by the number of tokens in a request and response. The number of tokens is dependant on the model being used. You see each model and its token limit on OpenAI's website on their [Models Overview page](https://platform.openai.com/docs/models/overview).

The class that manages conversational history is called [ChatBuilder](https://github.com/Azure/BuildYourOwnCopilot/blob/main/src/SemanticKernel/Chat/ChatBuilder.cs). This class is used to gather the most convesation history up to the token limits defined in configuration, then returns it as a string separating each prompt and completion with a new line character. The new line is not necessary for ChatGPT, but makes it more readable for a user when debugging. This method also returns the number of tokens used in the conversation. This value is used when building the prompt to send.

### Vectorizing the user prompt and conversation history

In a vector search solution, the filter predicate for any query is an array of vectors. This means that the text the user types in to the chat window, plus any conversational context that is gathered, must first be vectorized before the vector search can be done. This is accomplished in the [SemanticKernelRAGService](https://github.com/Azure/BuildYourOwnCopilot/blob/main/src/Infrastructure/Services/SemanticKernelRAGService.cs) in the solution in the [GetResponse()](https://github.com/Azure/BuildYourOwnCopilot/blob/main/src/Infrastructure/Services/SemanticKernelRAGService.cs#L127) method. This method takes a string and returns an array of vectors, along with the number of tokens used by the service.

### Doing the vector search

The vector search is the key function in this solution and is done against the Azure Cosmos DB vector store collection in this solution. The function itself is rather simple and only takes and array of vectors with which to do the search. You can see the vector search at work by debugging the Azure Web App remotely or running locally. Set a break point on [GetNearestMatchesAsync()](https://github.com/Azure/BuildYourOwnCopilot/blob/main/src/SemanticKernel/Connectors/Connectors.Memory/AzureCosmosDBNoSql/AzureCosmosDBNoSqlMemoryStore.cs#L41), then step through each line to see how of the function calls to see the search and returned data.

### Token management

One of the more challenging aspects to building RAG Pattern solutions is managing the tokens to stay within the maximum number of tokens that can be consumed in a single request (prompt) and response (completion). It's possible to build a prompt that consumes all of the tokens in the requests and leaves too few to produce a useful response. It's also possible to generate an exception from the Azure OpenAI Service if the request itself is over the token limit. You will need a way to measure token usage before sending the request. This is handled in the [OptimizePromptSize()](https://github.com/Azure/BuildYourOwnCopilot/blob/main/src/SemanticKernel/Chat/ChatBuilder.cs#L107) method in the ChatBuilder class. This method uses the SemanticKernel tokenizer, [GPT3Tokenizer](https://github.com/Azure/BuildYourOwnCopilot/blob/main/src/SemanticKernel/Chat/SemanticKernelTokenizer.cs). The utility takes text and generates an array of vectors. The number of elements in the array represent the number of tokens that will be consumed. It can also do the reverse and take an array of vectors and output text. In this method here we first generate the vectors on the data returned from our vector search, then if necessary, reduce the amount of data by calculating the number of vectors we can safely pass in our request to Azure OpenAI Service. Here is the flow of this method.

1. Measure the amount of tokens for the vector search results (rag data).
2. Measure the amount of tokens for the user prompt. This data is also used to capture what the user prompt tokens would be if processed without any additional data and stored in the user prompt message in the completions collection (more on that later).
3. Calculate if the amount of tokens used by the `search results` plus the `user prompt` plus the `conversation` + `completion` is greater than what the model will accept. If it is greater, then calculate how much to reduce the amount of data and `decode` the vector array we generated from the search results, back into text.
4. Finally, return the text from our search results as well as the number of tokens for the last User Prompt (this will get stored a bit later).

### Generate the completion

This is the most critical part of this entire solution, generating a chat completion from Azure OpenAI Service using one of its [GPT models](https://platform.openai.com/docs/guides/gpt) wherein the Azure OpenAI Service will take in all of the data we've gathered up to this point, then generate a response or completion which the user will see. All of this happens in the [SemanticKernelRAGService](https://github.com/Azure/BuildYourOwnCopilot/blob/main/src/Infrastructure/Services/SemanticKernelRAGService.cs) in the [GetResponse()](https://github.com/Azure/BuildYourOwnCopilot/blob/main/src/Infrastructure/Services/SemanticKernelRAGService.cs#L127) method. 

This method takes the user prompt and the search results and builds a `System Prompt` with the search data, as well as a user prompt that includes the conversation history plus the user's last question (prompt). The call is then made to the service which returns a `Chat Completion` object which contains the response text itself, plus the number of tokens used in the request (prompt) and the number of tokens used to generate the response (completion). 

One thing to note here is it is necessary to separate the number of tokens from the Prompt with the data versus the number of tokens from the text the user types into the chat interface. This is due to the need to accurately estimate the number of tokens for *just the text* of the user prompt and not for the data.

### Saving the results

The last part is to save the results of both the user prompt and completion as well as the amount of tokens used. All of the conversational history and the amount of tokens used in each prompt and completion is stored in the completions collection in the Azure Cosmos DB database in this solution. The call to the service is made by another method within ChatService called [AddPromptCompletionMessagesAsync()](https://github.com/Azure/BuildYourOwnCopilot/blob/main/src/Infrastructure/Services/ChatService.cs#L164). This method creates two new [Message](https://github.com/Azure/BuildYourOwnCopilot/blob/main/src/Infrastructure/Models/Chat/Message.cs) objects and stores them in a local cache of all the Sessions and Messages for the application. It then adds up all of the tokens used in the [Session](https://github.com/Azure/BuildYourOwnCopilot/blob/main/src/Infrastructure/Models/Chat/Session.cs) object which keeps a running total for the entire session.

The data is then persisted to the Cosmos DB database in the [UpdateSessionBatchAsync()](https://github.com/Azure/BuildYourOwnCopilot/blob/main/src/Infrastructure/Services/CosmosDbService.cs#L355) method. This method creates a new transaction then updates the Session document and inserts two new Message documents into the completions collection.

## Semantic Cache

Include text here on how the cache works.