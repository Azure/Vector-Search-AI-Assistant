Param(
    [parameter(Mandatory=$true)][string]$resourceGroup,
    [parameter(Mandatory=$true)][string]$cosmosDbAccountName, 
    [parameter(Mandatory=$true)][string]$subscription
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

$module = Get-InstalledModule -Name 'CosmosDB'
if($module -ne $null)
{
    write-host "Module CosmosDB is avaiable"
}
else
{
    write-host "Module CosmosDB is not avaiable, installing..."
    Install-Module -Name CosmosDB -AllowClobber -force
}

Import-Module CosmosDB

# Write-Host "Choosing your subscription" -ForegroundColor 
az account show
az account set --subscription $subscription

$database = "database"

$cosmosDbContext = New-CosmosDbContext -Account $cosmosDbAccountName -Database $database -ResourceGroup $resourceGroup

foreach($product in $products)
{
    New-CosmosDbDocument -Context $cosmosDbContext -CollectionId 'product' -DocumentBody ($product | ConvertTo-Json) -PartitionKey $product.categoryId
}

foreach($customer in $customers)
{
    New-CosmosDbDocument -Context $cosmosDbContext -CollectionId 'customer' -DocumentBody ($customer | ConvertTo-Json) -PartitionKey $customer.customerId
}

Pop-Location
