# Deployment

Users can deploy this solution from three locations, local machine, virtual machine, or from Cloud Shell. See [Deployment choices](#deployment-choices) for more information on why you would use those two installation options instructions. By default this should install from your local machine so you can have the code locally to run and debug.

## Prerequisites

- Azure Subscription
- Subscription access to Azure OpenAI service. Start here to [Request Access to Azure OpenAI Service](https://aka.ms/oaiapply)
- .NET 7 SDK
- Docker Desktop
- Azure CLI ([v2.51.0 or greater](https://docs.microsoft.com/cli/azure/install-azure-cli))
- Cross-platform (not Windows) PowerShell ([7.0 or greater](https://learn.microsoft.com/en-us/powershell/scripting/install/installing-powershell))
- [Helm 3.11.1 or greater](https://helm.sh/docs/intro/install/) (for AKS deployment)
- Visual Studio 2022 (only needed if you plan to run/debug the solution locally)

>**NOTE**: Installation requires the choice of an Azure Region. Make sure to set region you select which is used in the `<location>` value below supports Azure OpenAI services.  See [Azure OpenAI service regions](https://azure.microsoft.com/explore/global-infrastructure/products-by-region/?products=cognitive-services&regions=all) for more information.

## Deployment steps

Follow the steps below to deploy the solution to your Azure subscription.

1. Ensure all the prerequisites are installed.  Check to make sure you have the `Owner` role for the subscription assigned to your account.

2. Clone the repository:

    ```cmd
    git clone https://github.com/Azure/BuildYourOwnCopilot.git
    ```

3. Switch to the `main` branch:

    ```cmd
    cd BuildYourOwnCopilot
    git checkout main
    ```

> [!IMPORTANT]
> **Before continuing**, make sure have enough Tokens Per Minute (TPM) in thousands quota available in your subscription. By default, the script will attempt to set a value of 120K for each deployment. In case you need to change this value, you can edit the `params.deployments.sku.capacity` values (lines 131 and 142 in the `aca\infra\main.bicep` file for **ACA** deployments, or lines 141 and 152 in the `aks\infra\main.bicep` file for **AKS** deployments).

4. Run the following script to provision the infrastructure and deploy the API and frontend. This will provision all of the required infrastructure, deploy the API and web app services into your choice of Azure Kubeternetes Service or Azure Container Apps, and import data into Azure Cosmos DB.

    ### Deploy with Azure Kubernetes Service

    This script will deploy all services including a new Azure OpenAI account and AKS

    ```pwsh
    cd ./aks
    azd up
    ```

    You will be prompted for the target subscription, location, and desired environment name.  The target resource group will be `rg-` followed by the environment name (i.e. `rg-my-aks-deploy`)

    To validate the deployment using AKS run the following script. When the script it complete it will also output this value. You can simply click on it to launch the app.

    > ```pwsh
    >  az aks show -n <aks-name> -g <resource-group-name> -o tsv --query addonProfiles.httpApplicationRouting.config.HTTPApplicationRoutingZoneName
    >  ```

    After running `azd up` and the deployment finishes, you will see the output of the script which will include the URL of the web application. You can click on this URL to open the web application in your browser. The URL is beneath the "Done: Deploying service web" message, and is the second endpoint (the Ingress endpoint of type `LoadBalancer`).
    
    ![The terminal output after azd up completes shows the endpoint links.](../media/azd-aks-complete-output.png)
    
    If you closed the window and need to find the external IP address of the service, you can open the Azure portal, navigate to the resource group you deployed the solution to, and open the AKS service. In the AKS service, navigate to the `Services and Ingress` blade, and you will see the external IP address of the LoadBalancer service, named `nginx`:
    
    ![The external IP address of the LoadBalancer service is shown in the Services and Ingress blade of the AKS service.](../media/aks-external-ip.png)

    ### Deploy with Azure Container Apps

    This script will deploy all services including a new Azure OpenAI account using Azure Container Apps. (This can be a good option for users not familiar with AKS)

    ```pwsh
    cd ./aca
    azd up
    ```

    You will be prompted for the target subscription, location, and desired environment name. The target resource group will be `rg-` followed by the environment name (i.e. `rg-my-aca-deploy`)

    To validate the deployment to ACA run the following script:

    > ```pwsh
    >  az containerapp show -n <aca-name> -g <resource-group-name>
    >  ```

    After running `azd up` on the **ACA** deployment and the deployment finishes, you can locate the URL of the web application by navigating to the deployed resource group in the Azure portal. Click on the link to the new resource group in the output of the script to open the Azure portal.
    
    ![The terminal output aafter azd up completes shows the resource group link.](../media/azd-aca-complete-output.png)
    
    In the resource group, you will see the `ca-search-xxxx` Azure Container Apps service.
    
    ![The Search Azure Container App is highlighted in the resource group.](../media/search-container-app-resource-group.png)
    
    Select the service to open it, then select the `Application Url` to open the web application in your browser.
    
    ![The Application Url is highlighted in the Search Azure Container App overview blade.](../media/search-container-app-url.png)

> [!IMPORTANT]
> If you encounter any errors during the deployment, rerun `azd up` to continue the deployment from where it left off. This will not create duplicate resources, and tends to resolve most issues.

## Deployment choices

The following table summarizes the deployment choices available for the solution:

 Deployment type | Description | When to use
--- | --- | ---
[Standard](./deployment-standard.md) | Use your local development environment to deploy the solution to your Azure subscription. | Best suited for situations where you need the flexibility of a full development environment (e.g. to customize the solution) and you have a local development environment available.
[Cloud Shell](./deployment-cloudshell.md) | Use Azure Cloud Shell to deploy the solution to your Azure subscription. | Best suited for quick deployment. All you need is an Azure subscription and a browser. However, this does require additional setup steps. For more information see, [Prepare Cloud Shell Setup](./deployment-cloudshell-setup.md)
[Azure VM](./deployment-azurevm.md) | Use an Azure VM to deploy the solution to your Azure subscription. | Best suited for situations where you need the flexibility of a full development environment (e.g. to customize the solution) but you don't have a local development environment available. The Azure VM deployment type requires additional setup steps. If you are involved in managing the infrastructure that enables Azure VM deployments for your team, see [Prepare Azure VM Setup](./deployment-azurevm-setup.md) for more information.

## Deployment validation

Use the steps below to validate that the solution was deployed successfully.

Once the deployment script completes, the Application Insights `traces` query should display the following sequence of events:

![API initialization sequence of events](../media/initialization-trace.png)

Next, you should be able to see multiple entries referring to the vectorization of the data that was imported into Cosmos DB:

![API vectorization sequence of events](../media/initialization-embedding.png)

Finally, you should be able to see the Azure Cosmos DB vector store collection being populated with the vectorized data:

![Cosmos DB vector store collection populated with vectorized data](../media/initialization-vector-index.png)

>**NOTE**:
>
>It takes several minutes until all imported data is vectorized and indexed.

## Monitoring with Application Insights

Use the steps below to monitor the solution with Application Insights:

1. Navigate to the `Application Insights` resource that was created as part of the deployment.

2. Select the `Logs` section and create a new query with the following statement. Change the "Time range" setting on top tool bar to reflect the required time range. Click the `Run` button to execute the query:

    ```kql
    traces
    | order by timestamp desc
    ```

    ![Application Insights query](../media/monitoring-traces.png)

3. Select the `Export` button to explort the results the query.

4. In the query, replace `traces` with `requests` or `exceptions` to view the corresponding telemetry.
