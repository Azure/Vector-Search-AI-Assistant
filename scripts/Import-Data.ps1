Param(
    [parameter(Mandatory=$true)][string]$resourceGroup,
    [parameter(Mandatory=$true)][string]$location
)

Push-Location $($MyInvocation.InvocationName | Split-Path)
Push-Location ..
Remove-Item -Path dMT -Recurse -Force -ErrorAction Ignore
New-Item -ItemType Directory -Force -Path "dMT"
Push-Location "dMT"

$dmtUrl="https://github.com/AzureCosmosDB/data-migration-desktop-tool/releases/download/2.1.1/dmt-2.1.1-win-x64.zip"
Invoke-WebRequest -Uri $dmtUrl -OutFile dmt.zip
Expand-Archive -Path dmt.zip -DestinationPath .
Push-Location "windows-package"
Copy-Item -Path "../../migrationsettings.json" -Destination "./migrationsettings.json" -Force
Start-Process -FilePath "dmt.exe" -Wait

Pop-Location
Pop-Location
Pop-Location
Pop-Location
