# Solution Quickstart

Follow the steps below for a simple walk-through of the application and its capabilities.

@ciprianjichici, please add steps for exercising the data here more thoroughly with prompts on customers and sales orders. Also include steps for seeing the cache in action, changing min relevance, etc.

## Sample User Prompts

1. Browse to the web application for the solution in your browser.
1. Click [+ Create New Chat] button to create a new chat session.
1. Type in your questions in the text box and press Enter.

Here are some sample questions you can ask:

- What kind of socks do you have available?
- Do you have any customers from Canada? Where in Canada are they from?
- What kinds of bikes are in your product inventory?

- add more here


## Exploring the Semantic Cache

User types in some prompts for data that gets cached.
In new session test similar prompts, observe results.
Change similarity score. Start new sequence of prompts and completions, see the impact on cache hits.

etc...


## Handling real-time data

One great reason for using an operational database like Azure Cosmos DB as a source for your data in Generative AI applications is that you can leverage its Change Feed capability to dynamically add and remove records which can be vectorized and available in real-time. The steps below can demonstrate this capability.

### Steps to demo adding and removing data from vector database

1. Start a new chat session in the web application.
1. In the chat text box, type: "Can you list all of your socks?". The AI Assistant will list 4 different socks of two types, racing and mountain.
1. Using either CURL or Postman, send the following payload in a PUT request with a `Content-Type` header value of `application/json` to `https://<chat-service-hostname>/api/products` to add a product.
  
    ##### Curl Command
    ```pwsh
    curl -X PUT -H "Content-Type: application/json" -d $JsonPayload https://<chat-service-hostname>/api/products
    ```

    ##### Json Payload
    ```json
    {
        "id": "00001",
        "categoryId": "C48B4EF4-D352-4CD2-BCB8-CE89B7DFA642",
        "categoryName": "Clothing, Socks",
        "sku": "SO-R999-M",
        "name": "Cosmic Racing Socks, M",
        "description": "The product called Cosmic Racing Socks, M",
        "price": 6.00,
        "tags": [
            {
                "id": "51CD93BF-098C-4C25-9829-4AD42046D038",
                "name": "Tag-25"
            },
            {
                "id": "5D24B427-1402-49DE-B79B-5A7013579FBC",
                "name": "Tag-76"
            },
            {
                "id": "D4EC9C09-75F3-4ADD-A6EB-ACDD12C648FA",
                "name": "Tag-153"
            }
        ]
    }
    ```
    > Note the `id` of `00001` and `categoryId` of `C48B4EF4-D352-4CD2-BCB8-CE89B7DFA642`.  We will need these values in a later step.


1. Return to the AI Assistant and type, ""Can you list all of your socks again?". This time you should see a new product, "Cosmic Socks, M"
1. Using either CURL or Postman, send the following payload in a DELETE request to `https://<chat-service-hostname>/products/<product_id>?categoryId=<category_id>` to add a product, where `<product_id>` is the value of the `id` field and `<category_id>` is the value of the `categoryId` field of the JSON payload sent via a PUT request in a previous step (`00001` and `C48B4EF4-D352-4CD2-BCB8-CE89B7DFA642`, respectively, in this case).

    ##### Curl Command
    ```pwsh
    curl -X DELETE https://<chat-service-hostname>/products/<product_id>?categoryId=<category_id>
    ```

1. Open a **new** chat session and ask the same question again. This time it should show the original list of socks in the product catalog. 

**Note:** Using the same chat session after adding them will sometimes result in the Cosmic Socks not being returned. If that happens, start a new chat session and ask the same question. Also, sometimes after removing the socks they will continue to be returned by the AI Assistant. If that occurs, also start a new chat session. The reason this occurs is that previous prompts and completions are sent to OpenAI to allow it to maintain conversational context. Because of this, it will sometimes use previous completions as a background for generating subsequent responses.

<p align="center">
    <img src="media/socks.png" width="100%">
</p>
