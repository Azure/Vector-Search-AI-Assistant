# Deployment - Standard

## Prerequisites

- Azure Subscription
- Subscription access to Azure OpenAI service. Start here to [Request Access to Azure OpenAI Service](https://customervoice.microsoft.com/Pages/ResponsePage.aspx?id=v4j5cvGGr0GRqy180BHbR7en2Ais5pxKtso_Pz4b1_xUOFA5Qk1UWDRBMjg0WFhPMkIzTzhKQ1dWNyQlQCN0PWcu)
- .NET 7 SDK
- Docker Desktop
- Azure CLI ([v2.51.0 or greater](https://docs.microsoft.com/cli/azure/install-azure-cli))
- [Helm 3.11.1 or greater](https://helm.sh/docs/intro/install/) (for AKS deployment)
- Visual Studio 2022 (only needed if you plan to run/debug the solution locally)

>**NOTE**: Installation requires the choice of an Azure Region. Make sure to set region you select which is used in the `<location>` value below supports Azure OpenAI services.  See [Azure OpenAI service regions](https://azure.microsoft.com/explore/global-infrastructure/products-by-region/?products=cognitive-services&regions=all) for more information.

## Deployment steps

Follow the steps below to deploy the solution to your Azure subscription.

1. Ensure all the prerequisites are installed.  

2. Clone the repository:
   
    ```cmd
    git clone https://github.com/Azure/Vector-Search-AI-Assistant.git
    ```

3. Switch to the `cognitive-search-vector` branch:

    ```cmd
    cd Vector-Search-AI-Assistant
    git checkout cognitive-search-vector
    ```

4. Run the following script to provision the infrastructure and deploy the API and frontend. This will provision all of the required infrastructure, deploy the API and web app services into your choice of Azure Kubeternetes Service or Azure Container Apps, and import data into Azure Cosmos DB.

    ### Deploy with Azure Kubernetes Service
    This script will deploy all services including a new Azure OpenAI account and AKS

    ```pwsh
    ./scripts/Unified-Deploy.ps1 -resourceGroup <rg_name> -location <location> -subscription <target_subscription_id> -deployAks 1
    ```

    ### Deploy with pre-existing Azure OpenAI service with Azure Kubernetes Service
    This script will deploy using a pre-existing Azure OpenAI account and pre-deployed GPT 3.5 Turbo and ADA-002 models and AKS

    ```pwsh
    ./scripts/Unified-Deploy.ps1 -resourceGroup <rg_name> -location <location> `
        -subscription <target_subscription_id> -deployAks 1 `
        -openAiName <openai-account> `
        -openAiRg <openai-rg-name> `
        -openAiCompletionsDeployment <gpt-model-name> `
        -openAiEmbeddingsDeployment <ada-002-model-name>
    ```


### Deploy with Azure Container Apps
    This script will deploy all services including a new Azure OpenAI account using Azure Container Apps. (This can be a good option for users not familiar with AKS)

    ```pwsh
    ./scripts/Unified-Deploy.ps1 -resourceGroup <rg_name> -location <location> -subscription <target_subscription_id> -deployAks 0
    ```

    ### Deploy with pre-existing Azure OpenAI service with Azure Container Apps
    This script will deploy using a pre-existing Azure OpenAI account and pre-deployed GPT 3.5 Turbo and ADA-002 models and AKS

    ```pwsh
    ./scripts/Unified-Deploy.ps1 -resourceGroup <rg_name> -location <location> `
        -subscription <target_subscription_id> -deployAks 0 `
        -openAiName <openai-account> `
        -openAiRg <openai-rg-name> `
        -openAiCompletionsDeployment <gpt-model-name> `
        -openAiEmbeddingsDeployment <ada-002-model-name>
    ```

