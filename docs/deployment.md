# Deployment

## Deployment choices

The following table summarizes the deployment choices available for the solution:

 Deployment type | Description | When to use
--- | --- | ---
[Cloud Shell](./deployment-cloudshell.md) | Use Azure Cloud Shell to deploy the solution to your Azure subscription. | Best suited for quick deployment. All you need is an Azure subscription and a browser.
[Azure VM](./deployment-azurevm.md) | Use an Azure VM to deploy the solution to your Azure subscription. | Best suited for situations where you need the flebility of a full development environment (e.g. to customize the solution) but you don't have a local development environment available.
[Standard](./deployment-standard.md) | Use your local development environment to deploy the solution to your Azure subscription. | Best suited for situations where you need the flebility of a full development environment (e.g. to customize the solution) and you have a local development environment available.

Select the links in the table above to learn more about each deployment choice.

>**NOTE**:
>The Cloud Shell deployment type requires additional setup steps. If you are involved in managing the infrastructure that enables cloud shell deployments for your team, see [Prepare Cloud Shell Setup](./deployment-cloudshell-setup.md) for more information.

>**NOTE**:
>The Azure VM deployment type requires additional setup steps. If you are involved in managing the infrastructure that enables Azure VM deployments for your team, see [Prepare Azure VM Setup](./deployment-azurevm-setup.md) for more information.

## Deployment validation

Use the steps below to validate that the solution was deployed successfully.

Once the deployment script completes, the Application Insights `traces` query should display the following sequence of events:

![API initialization sequence of events](../img/initialization-trace.png)

Next, you should be able to see multiple entries referring to the vectorization of the data that was imported into Cosmos DB:

![API vectorization sequence of events](../img/initialization-embedding.png)

Finally, you should be able to see the Cognitive Search index being populated with the vectorized data:

![Cognitive Search index populated with vectorized data](../img/initialization-vector-index.png)

>**NOTE**:
>
>It takes several minutes until all imported data is vectorized and indexed.