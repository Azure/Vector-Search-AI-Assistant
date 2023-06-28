#!/usr/bin/pwsh

Param(
    [parameter(Mandatory=$false)][string]$name,
    [parameter(Mandatory=$true)][string]$resourceGroup,
    [parameter(Mandatory=$true)][string]$location,
    [parameter(Mandatory=$false)][string]$completionsDeployment,
    [parameter(Mandatory=$false)][string]$embeddingsDeployment
)

Push-Location $($MyInvocation.InvocationName | Split-Path)

if ($name) {
    $openAi=$(az cognitiveservices account show -g $resourceGroup -n $name -o json | ConvertFrom-Json)
    if (-not $openAi) {
        $openAi=$(az cognitiveservices account create -g $resourceGroup -n $name --kind OpenAI --sku S0 --location $location --yes -o json | ConvertFrom-Json)
    }
} else {
    $openAi=$(az cognitiveservices account list -g $resourceGroup -o json | ConvertFrom-Json)[0]
}

if ($completionsDeployment) {
    $openAiDeployment=$(az cognitiveservices account deployment show -g $resourceGroup -n $openAi.name --deployment-name $completionsDeployment)
    if (-not $openAiDeployment) {
        $openAiDeployment=$(az cognitiveservices account deployment create -g $resourceGroup -n $openAi.name --deployment-name $completionsDeployment --model-name 'gpt-35-turbo' --model-version '0301' --model-format OpenAI)
    }
} else {
    $completionsDeployment='completions'
    $openAiDeployment=$(az cognitiveservices account deployment show -g $resourceGroup -n $openAi.name --deployment-name $completionsDeployment)
}

if ($embeddingsDeployment) {
    $openAiDeployment=$(az cognitiveservices account deployment show -g $resourceGroup -n $openAi.name --deployment-name $embeddingsDeployment)
    if (-not $openAiDeployment) {
        $openAiDeployment=$(az cognitiveservices account deployment create -g $resourceGroup -n $openAi.name --deployment-name $embeddingsDeployment --model-name 'text-embedding-ada-002' --model-version '2' --model-format OpenAI)
    }
} else {
    $embeddingsDeployment='embeddings'
    $openAiDeployment=$(az cognitiveservices account deployment show -g $resourceGroup -n $openAi.name --deployment-name $embeddingsDeployment)
}

Pop-Location