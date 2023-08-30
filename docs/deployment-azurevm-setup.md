# Prepare Azure VM Setup

Before users in your team can deploy the solution using Azure VM, you need to perform the following steps:

1. Create an Azure Storage account in the target subscription.

2. Create a publicly accessible container named `vmscripts` in the Azure Storage account created in step 1.

3. Clone the repository:
   
    ```cmd
    git clone https://github.com/AzureCosmosDB/VectorSearchAiAssistant.git
    ```

4. Switch to the `cognitive-search-vector` branch:

    ```cmd
    cd VectorSearchAiAssistant
    git checkout cognitive-search-vector
    ```

5. Upload the `VMScriptExtension.ps1` script from the `scripts` folder to the `vmscripts` container created in step 2. This script is used by the Azure VM deployment script to install the required software on the VM.

6. Open the `vmdeploy.json` file from the `arm` folder with the text editor of your choice. In line 219, update the value of the `fileUris` property to point to the `VMScriptExtension.ps1` script uploaded in step 5.

7. Save the changes to the `vmdeploy` script, commit them to the `cognitive-search-vector` branch, and push the changes to the remote repository.

    ```cmd
    git commit -m "Updated VM extensions script for Azure VM deployment"
    git push
    ```
