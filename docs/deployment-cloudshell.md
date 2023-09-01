# Deployment - Cloud shell

## Prerequisites

- Azure subscription
- Subscription access to Azure OpenAI service. Start here to [Request Access to Azure OpenAI Service](https://customervoice.microsoft.com/Pages/ResponsePage.aspx?id=v4j5cvGGr0GRqy180BHbR7en2Ais5pxKtso_Pz4b1_xUOFA5Qk1UWDRBMjg0WFhPMkIzTzhKQ1dWNyQlQCN0PWcu)
- Azure Cloud Shell environment (follow [these instructions](https://learn.microsoft.com/en-us/azure/cloud-shell/quickstart?tabs=azurecli) to setup your Cloud Shell)

## Deployment steps

Follow the steps below to deploy the solution to your Azure subscription.

1. Create a cloud shell PowerShell environment in a tenant that contains the target subscription.  

2. Clone the repository:
   
    ```cmd
    git clone https://github.com/AzureCosmosDB/VectorSearchAiAssistant.git
    ```

3. Switch to the `cognitive-search-vector` branch:

    ```cmd
    cd VectorSearchAiAssistant
    git checkout cognitive-search-vector
    ```

4. Set the proper folder permissions on the `scripts` folder:

    ```cmd
    chmod +x ./scripts/*
    ```

5.  Execute the `CloudShell-Deploy.ps1` script. This will provision all of the required infrastructure, deploy the API and web app services into AKS, and import data into Cosmos DB.

    ```pwsh
    ./scripts/CloudShell-Deploy.ps1 -resourceGroup <rg_name> -location <location> -subscription <target_subscription_id>
    ```

    This is an example of the command above: 
    ```pwsh
    ./scripts/CloudShell-Deploy.ps1 -resourceGroup "ms-cosmosdb-openai" -location eastus -subscription "00000000-0000-0000-0000-000000000000"
    ```

>**NOTE**: The `<location>` specified must point to a region that supports the Azure OpenAI service. You can find the list of supported regions [here](https://azure.microsoft.com/en-us/explore/global-infrastructure/products-by-region/?products=cognitive-services).

>**NOTE**: If you already used the Cloud Shell in the target subscription, it is recommended to remove the `VectorSearchAiAssistant` folder from the home directory before cloning the repository again. You can do this by running `rm ./VectorSearchAiAssistant -r -f` in the Cloud Shell.