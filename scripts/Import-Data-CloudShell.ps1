#! /usr/bin/pwsh

Param(
    [parameter(Mandatory=$true)][string]$resourceGroup,
    [parameter(Mandatory=$true)][string]$aksName
)

Push-Location $($MyInvocation.InvocationName | Split-Path)

$blobUri = "https://cosmosdbcosmicworks.blob.core.windows.net/cosmic-works-small/product.json"
$result = Invoke-WebRequest -Uri $blobUri
$products = $result.Content | ConvertFrom-Json
Write-Output "Imported $($products.Length) products"

$blobUri = "https://cosmosdbcosmicworks.blob.core.windows.net/cosmic-works-small/customer.json"
$result = Invoke-WebRequest -Uri $blobUri
# The customers file has a BOM which needs to be ignored
$customers = $result.Content.Substring(1, $result.Content.Length - 1) | ConvertFrom-Json
Write-Output "Imported $($customers.Length) customers"

$webappHostname=$(az aks show -n $aksName -g $resourceGroup -o json --query addonProfiles.httpApplicationRouting.config.HTTPApplicationRoutingZoneName | ConvertFrom-Json)
$apiUrl = "https://$webappHostname/api"

$OldProgressPreference = $ProgressPreference
$ProgressPreference = "SilentlyContinue"

foreach($product in $products)
{
    Invoke-RestMethod -Uri $apiUrl/products -Method POST -Body ($product | ConvertTo-Json) -ContentType 'application/json'
}

foreach($customer in $customers)
{
    if ($customer.type -eq "customer") {
        Invoke-RestMethod -Uri $apiUrl/customers -Method POST -Body ($customer | ConvertTo-Json) -ContentType 'application/json'
    } elseif ($customer.type -eq "salesOrder") {
        Invoke-RestMethod -Uri $apiUrl/salesorders -Method POST -Body ($customer | ConvertTo-Json) -ContentType 'application/json'
    }
}

$ProgressPreference = $OldProgressPreference

Pop-Location
