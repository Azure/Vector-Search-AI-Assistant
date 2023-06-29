#! /usr/bin/pwsh

Param(
    [parameter(Mandatory=$true)][string]$resourceGroup,
    [parameter(Mandatory=$true)][string]$location,
    [parameter(Mandatory=$true)][string]$template
)

$sourceFolder=$(Join-Path -Path .. -ChildPath arm)

Push-Location $($MyInvocation.InvocationName | Split-Path)

$script=$template

Write-Host "--------------------------------------------------------" -ForegroundColor Yellow
Write-Host "Deploying ARM script $script" -ForegroundColor Yellow
Write-Host "-------------------------------------------------------- " -ForegroundColor Yellow

$rg = $(az group show -n $resourceGroup -o json | ConvertFrom-Json)
# Deployment without AKS can be done in a existing or non-existing resource group.
if (-not $rg) {
    Write-Host "Creating resource group $resourceGroup in $location" -ForegroundColor Yellow
    az group create -n $resourceGroup -l $location
}

Write-Host "Getting last AKS version in location $location" -ForegroundColor Yellow
$aksVersions=$(az aks get-versions -l $location --query  values[].version -o json | ConvertFrom-Json)
$aksLastVersion=$aksVersions[$aksVersions.Length-1]
Write-Host "AKS last version is $aksLastVersion" -ForegroundColor Yellow

Write-Host "Begining the ARM deployment..." -ForegroundColor Yellow
Push-Location $sourceFolder
az deployment group create -g $resourceGroup --template-file $script --parameters k8sVersion=$aksLastVersion
Pop-Location 
Pop-Location 
