# Deployment - Standard

## Prerequisites

- Azure Subscription
- Subscription access to Azure OpenAI service. Start here to [Request Access to Azure OpenAI Service](https://customervoice.microsoft.com/Pages/ResponsePage.aspx?id=v4j5cvGGr0GRqy180BHbR7en2Ais5pxKtso_Pz4b1_xUOFA5Qk1UWDRBMjg0WFhPMkIzTzhKQ1dWNyQlQCN0PWcu)
- .NET 7 SDK
- Docker Desktop
- Azure CLI ([v2.49.0 or greater](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli))
- [Helm 3.11.1 or greater](https://helm.sh/docs/intro/install/)
- Visual Studio 2022 (only needed if you plan to run/debug the solution locally)

## Deployment steps

Follow the steps below to deploy the solution to your Azure subscription.

1. Ensure all the prerequisites are installed.  

2. Clone the repository:
   
    ```cmd
    git clone https://github.com/AzureCosmosDB/VectorSearchAiAssistant.git
    ```

3. Switch to the `cognitive-search-vector` branch:

    ```cmd
    cd VectorSearchAiAssistant
    git checkout cognitive-search-vector
    ```

4. Run the following script to provision the infrastructure and deploy the API and frontend. This will provision all of the required infrastructure, deploy the API and web app services into AKS, and import data into Cosmos DB.

    ```pwsh
    ./scripts/Unified-Deploy.ps1 -resourceGroup <rg_name> -location <location> -subscription <target_subscription_id>
    ```

>**NOTE**: Make sure to set the `<location>` value to a region that supports Azure OpenAI services.  See [Azure OpenAI service regions](https://azure.microsoft.com/en-us/explore/global-infrastructure/products-by-region/?products=cognitive-services&regions=all) for more information.
