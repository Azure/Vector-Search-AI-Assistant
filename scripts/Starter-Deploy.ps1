#! /usr/bin/pwsh

Param(
    [parameter(Mandatory=$false)][string]$acrName=$null,
    [parameter(Mandatory=$true)][string]$resourceGroup,
    [parameter(Mandatory=$true)][string]$location,
    [parameter(Mandatory=$true)][string]$subscription,
    [parameter(Mandatory=$true)][string]$armTemplate="starter-azuredeploy.json",
    [parameter(Mandatory=$false)][string]$openAiName=$null,
    [parameter(Mandatory=$false)][string]$openAiRg=$null,
    [parameter(Mandatory=$false)][string]$openAiCompletionsDeployment=$null,
    [parameter(Mandatory=$false)][string]$openAiEmbeddingsDeployment=$null,
    [parameter(Mandatory=$false)][bool]$stepDeployArm=$true,
    [parameter(Mandatory=$false)][bool]$stepDeployOpenAi=$true,
    [parameter(Mandatory=$false)][bool]$stepBuildPush=$false,
    [parameter(Mandatory=$false)][bool]$stepDeployCertManager=$true,
    [parameter(Mandatory=$false)][bool]$stepDeployTls=$true,
    [parameter(Mandatory=$false)][bool]$stepDeployImages=$false,
    [parameter(Mandatory=$false)][bool]$stepUploadSystemPrompts=$true,
    [parameter(Mandatory=$false)][bool]$stepImportData=$false,
    [parameter(Mandatory=$false)][bool]$stepLoginAzure=$true
)

Push-Location $($MyInvocation.InvocationName | Split-Path)

& ./Unified-Deploy.ps1 -acrName $acrName `
                       -resourceGroup $resourceGroup `
                       -location $location `
                       -subscription $subscription `
                       -armTemplate $armTemplate `
                       -openAiName $openAiName `
                       -openAiRg $openAiRg `
                       -openAiCompletionsDeployment $openAiCompletionsDeployment `
                       -openAiEmbeddingsDeployment $openAiEmbeddingsDeployment `
                       -stepDeployArm $stepDeployArm `
                       -stepDeployOpenAi $stepDeployOpenAi `
                       -stepBuildPush $stepBuildPush `
                       -stepDeployCertManager $stepDeployCertManager `
                       -stepDeployTls $stepDeployTls `
                       -stepDeployImages $stepDeployImages `
                       -stepUploadSystemPrompts $stepUploadSystemPrompts `
                       -stepImportData $stepImportData `
                       -stepLoginAzure $stepLoginAzure

Pop-Location
