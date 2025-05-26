$rootSiteUrl = "https://mngenvmcap512619.sharepoint.com" # replace with your root site URL
$containerTypeName = "ChatWithYourData"
$owningApplicationId = "06ab7aa3-526b-4d77-8614-c1a03b50d53d" # this is the AppId of the backend application
$azureSubscriptionId = "5b398137-467e-43bb-9c4b-a9de3fcb2c37"
$resourceGroup = "rg-spe" # this can be the same as the one used for the backend application
$region = "westeurope" # this can be the same as the one used for the backend application
$tenantId = 'f903e023-a92d-4561-9a3b-d8429e3fa1fd'

$containerTypeId = 'aaace084-a939-40a0-98f0-919307b365ab'

$containerType = New-SPOContainerType -ContainerTypeName $containerTypeName -OwningApplicationId $owningApplicationId
Add-SPOContainerTypeBilling -ContainerTypeId $containerType.ContainerTypeId -AzureSubscriptionId $azureSubscriptionId -ResourceGroup $resourceGroup

#needed to load the ConfidentialClientApplicationBuilder class
Install-Module -Name MSAL.PS -Force -AllowClobber -Scope CurrentUser
Import-Module MSAL.PS

$scopes = new-object System.Collections.Generic.List[string]
$scopes.Add("$rootSiteUrl/.default")
#$scopes.Add("https://graph.microsoft.com/.default")
$authority = "https://login.microsoftonline.com/$tenantId"

# certificate
$certpath = 'C:\Users\rmeure\OneDrive - Microsoft\Customers\_awesomescripts\APTSPE.pfx'
$certPassword = 'H;s[M.CL@3Wk6,-c%$X<K&'
$certificate = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2($certPath, $certPassword)

$builder = [Microsoft.Identity.Client.ConfidentialClientApplicationBuilder]::Create($owningApplicationId).WithCertificate($certificate)
$builder = $builder.WithAuthority($authority)
$app = $builder.Build()
$token = $app.AcquireTokenForClient($scopes).ExecuteAsync().Result.AccessToken

$headers =@{
    Authorization = "Bearer $token"
    #ConsistencyLevel = "eventual"
}
$body = @{
    "value" = @(
        @{
            "appId" = $owningApplicationId
            "delegated" = @("full")
            "appOnly" = @("full")
        }
    )
} | ConvertTo-Json -Depth 10
$body = $body -replace '"', "'"
$url = "$rootSiteUrl/_api/v2.1/storageContainerTypes/$containerTypeId/applicationPermissions"
Invoke-RestMethod -Uri $url -Headers $headers -Method Put -ContentType "application/json" -Body $body
