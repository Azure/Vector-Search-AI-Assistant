# Deployment - Azure VM

## Prerequisites

- Azure subscription
- Subscription access to Azure OpenAI service. Start here to [Request Access to Azure OpenAI Service](https://customervoice.microsoft.com/Pages/ResponsePage.aspx?id=v4j5cvGGr0GRqy180BHbR7en2Ais5pxKtso_Pz4b1_xUOFA5Qk1UWDRBMjg0WFhPMkIzTzhKQ1dWNyQlQCN0PWcu)

## Deployment steps

Run the following script to provision a development VM with Visual Studio 2022 Community and required dependencies preinstalled.

```pwsh
.\scripts\Deploy-Vm.ps1 -resourceGroup <rg_name> -location <location> -password <password>
```
`<password`> is the password for the `BYDtoChatGPTUser` account that will be created on the VM. It must be at least 12 characters long and meet the complexity requirements of Azure VMs.

When the script completes, the console output should display the name of the provisioned VM similar to the following:

```
The resource prefix used in deployment is libxarwttxjde
The deployed VM name used in deployment is libxarwttxjdevm
```

Use RDP to remote into the freshly provisioned VM with the username `BYDtoChatGPTUser` and the password you provided earlier on.  

>**IMPORTANT**: The password for the `BYDtoChatGPTUser` account must be changed on first login.

Open up a powershell terminal and run the following script to provision the infrastructure and deploy the API and frontend. This will provision all of the required infrastructure, deploy the API and web app services into AKS, and import data into Cosmos.

```pwsh
git clone https://github.com/AzureCosmosDB/VectorSearchAiAssistant.git
cd VectorSearchAiAssistant
git checkout cognitive-search-vector
./scripts/VmEnvironment-Deploy.ps1 -resourceGroup <rg-name> -location EastUS -subscription <target-subscription> -stepLoginAzure 1
```

>**NOTE**: The `<location>` specified must point to a region that supports the Azure OpenAI service. You can find the list of supported regions [here](https://azure.microsoft.com/en-us/explore/global-infrastructure/products-by-region/?products=cognitive-services).
