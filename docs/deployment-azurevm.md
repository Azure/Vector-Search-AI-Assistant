# Deployment - Azure VM

## Prerequisites

- Azure subscription
- Subscription access to Azure OpenAI service. Start here to [Request Access to Azure OpenAI Service](https://customervoice.microsoft.com/Pages/ResponsePage.aspx?id=v4j5cvGGr0GRqy180BHbR7en2Ais5pxKtso_Pz4b1_xUOFA5Qk1UWDRBMjg0WFhPMkIzTzhKQ1dWNyQlQCN0PWcu)

## Deployment steps

Follow the steps below to deploy the solution to your Azure subscription.

1. Run the following script to provision a development VM with Visual Studio 2022 Community and required dependencies preinstalled.

    ```pwsh
    .\scripts\Deploy-Vm.ps1 -resourceGroup <rg_name> -location <location> -password <password>
    ```

    `<password`> is the password for the `BYDtoChatGPTUser` account that will be created on the VM. It must be at least 12 characters long and meet the complexity requirements of Azure VMs.

    When the script completes, the console output should display the name of the provisioned VM similar to the following:

    ```txt
    The resource prefix used in deployment is libxarwttxjde
    The deployed VM name used in deployment is libxarwttxjdevm
    ```

2. Use RDP to remote into the freshly provisioned VM with the username `BYDtoChatGPTUser` and the password you provided earlier on.  

3. Add the `BYDtoChatGPTUser` account to the `docker-users` local group on the VM. Sign out and sign back in to the VM to apply the changes.

4. Install WSL2 by running the following command in a command prompt:

    ```cmd
    wsl --install
    ```

5. Restart the VM to complete the setup.

6. Log back in with the `BYDtoChatGPTUser` account and start `Docker Desktop`. Ensure the Docker engine is up and running. Keep `Docker Desktop` running in the background.

7. Clone the repository:

    ```cmd
    git clone https://github.com/AzureCosmosDB/VectorSearchAiAssistant.git
    ```

8. Switch to the `cognitive-search-vector` branch:

    ```cmd
    cd VectorSearchAiAssistant
    git checkout cognitive-search-vector
    ```

9. Run the following script to provision the infrastructure and deploy the API and frontend. This will provision all of the required infrastructure, deploy the API and web app services into AKS, and import data into Cosmos DB.

    ```pwsh
    ./scripts/Unified-Deploy.ps1 -resourceGroup <rg_name> -location <location> -subscription <target_subscription_id>
    ```

>**NOTE**: Make sure to set the `<location>` value to a region that supports Azure OpenAI services.  See [Azure OpenAI service regions](https://azure.microsoft.com/en-us/explore/global-infrastructure/products-by-region/?products=cognitive-services&regions=all) for more information.
