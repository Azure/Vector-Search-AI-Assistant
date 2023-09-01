# Prepare Cloud Shell Setup

Before users in your team can deploy the solution using Cloud Shell, you need to perform the following steps:

1. Create an Azure Container Registry (ACR) instance in the target subscription. Ensure anonymous pull access is enabled on the ACR instance (see [here](https://learn.microsoft.com/en-us/azure/container-registry/anonymous-pull-access) for more information).

2. Clone the repository:
   
    ```cmd
    git clone https://github.com/AzureCosmosDB/VectorSearchAiAssistant.git
    ```

3. Switch to the `cognitive-search-vector` branch:

    ```cmd
    cd VectorSearchAiAssistant
    git checkout cognitive-search-vector
    ```

4. Open the `CloudShell-Deploy.ps1` script from the `scripts` folder with the text editor of your choice. In lines 4 and 5, update the default values for the parameters `acrName` and `acrResourceGroup` with the values corresponding to the ACR instance created in step 1. 

5. Save the changes to the `CloudShell-Deploy.ps1` script, commit them to the `cognitive-search-vector` branch, and push the changes to the remote repository.

    ```cmd
    git commit -m "Updated ACR details for Cloud Shell deployment"
    git push
    ```
   
6. Execute the `Prepare-CloudShell-Deploy.ps1` script. This will build the portal and API Docker images and push them to the ACR instance created in step 1.


    ```pwsh
    ./scripts/Prepare-CloudShell-Deploy.ps1 -resourceGroup <rg_name> -acrName <acr_name> -subscription <target_subscription_id>
    ```

    `<rg_name>` is the name of the resource group where the ACR instance was created in step 1.

    `<acr_name>` is the name of the ACR instance created in step 1.

    `<target_subscription_id>` is the ID of the target subscription.

    This is an example of the command above: 
    ```pwsh
    ./scripts/Prepare-CloudShell-Deploy.ps1 -resourceGroup "ms-byd-to-chatgpt" -acrName "bydtochatgptcr" -subscription "00000000-0000-0000-0000-000000000000"
    ```

>**NOTE**:
>Make sure you pull the latest changes from the `cognitive-search-vector` branch and rerun step 4 above each time you want to update the portal and API Docker images in the ACR instance as a result of changes made to the code.