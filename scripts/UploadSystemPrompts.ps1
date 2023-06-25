#!/usr/bin/pwsh

Param(
    [parameter(Mandatory=$true)][string]$resourceGroup,
    [parameter(Mandatory=$true)][string]$location
)

Push-Location $($MyInvocation.InvocationName | Split-Path)
Push-Location ..

$storageAccount = $(az storage account list -g $resourceGroup -o json | ConvertFrom-Json)
az storage container create --account-name $storageAccount.name --name "system-prompt"
az storage azcopy blob upload -c system-prompt --account-name $storageAccount.name -s "../SystemPrompts/*" --recursive

Pop-Location
Pop-Location
