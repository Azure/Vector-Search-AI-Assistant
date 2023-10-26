#! /usr/bin/pwsh

Param(
    [parameter(Mandatory=$true)][string]$resourceGroup,
    [parameter(Mandatory=$true)][string]$location,
    [parameter(Mandatory=$false)][string]$template="azuredeploy.json",
    [parameter(Mandatory=$false)][string]$resourcePrefix,
    [parameter(Mandatory=$false)][string]$cosmosDbAccountName, 
    [parameter(Mandatory=$true)][string]$openAiEndpoint,
    [parameter(Mandatory=$true)][string]$openAiKey,
    [parameter(Mandatory=$true)][string]$openAiCompletionsDeployment,
    [parameter(Mandatory=$true)][string]$openAiEmbeddingsDeployment
)

$sourceFolder=$(Join-Path -Path .. -ChildPath arm)

Push-Location $($MyInvocation.InvocationName | Split-Path)

$script=$template

Write-Host "--------------------------------------------------------" -ForegroundColor Yellow
Write-Host "Deploying ARM script $script" -ForegroundColor Yellow
Write-Host "-------------------------------------------------------- " -ForegroundColor Yellow

$rg = $(az group list --query "[?name=='$resourceGroup']" -o json | ConvertFrom-Json)
# Deployment without AKS can be done in a existing or non-existing resource group.
if ($rg.length -eq 0) {
    Write-Host "Creating resource group $resourceGroup in $location" -ForegroundColor Yellow
    az group create -n $resourceGroup -l $location
}

$deploymentName = "cosmosdb-openai-azuredeploy"

Write-Host "Begining the ARM deployment..." -ForegroundColor Yellow
Push-Location $sourceFolder
az deployment group create -g $resourceGroup -n $deploymentName --template-file $script --parameters openAiEndpoint=$openAiEndpoint --parameters openAiKey=$openAiKey --parameters openAiCompletionsDeployment=$openAiCompletionsDeployment --parameters openAiEmbeddingsDeployment=$openAiEmbeddingsDeployment

$outputVal = (az deployment group show -g $resourceGroup -n $deploymentName --query properties.outputs.resourcePrefix.value) | ConvertFrom-Json
Set-Variable -Name resourcePrefix -Value $outputVal.ToString() -Scope 1
Write-Host "The resource prefix used in deployment is $outputVal"

$outputVal = (az deployment group show -g $resourceGroup -n $deploymentName --query properties.outputs.cosmosDbAccountName.value) | ConvertFrom-Json
Set-Variable -Name cosmosDbAccountName -Value $outputVal.ToString() -Scope 1
Write-Host "The CosmosDB account name used in deployment is $outputVal"

Pop-Location 
Pop-Location 
