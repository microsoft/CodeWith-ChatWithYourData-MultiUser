param (
    [string] [Parameter(Mandatory=$true)] $rgName,
    [string] [Parameter(Mandatory=$true)] $appServiceName
)

$initialDirectory = Get-Location

try {
    # Change to the react source directory
    cd ..\src\dotnet\DocApi

    # Run the build process
    dotnet publish --configuration Release
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed"
    }

    # Change to the dist directory
    cd .\bin\Release\net9.0\publish

    # Compress the build output
    Compress-Archive -Path * -DestinationPath app.zip -Force

    # Deploy to Azure App Service
    az webapp deploy --resource-group $rgName --name $appServiceName --src-path app.zip --async true
    if ($LASTEXITCODE -ne 0) {
        throw "Azure deployment failed"
    }
    Write-Output "API deployed successfully."

} catch {
    Write-Error $_.Exception.Message
    exit 1
} finally {
    Set-Location $initialDirectory
}