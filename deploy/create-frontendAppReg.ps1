<#
.SYNOPSIS
  Demonstrates creating an Azure AD app registration with:
    – Custom App ID URI
    – A custom scope named "chat" available to both admins and end users
    – Delegated permission (User.Read) on Microsoft Graph

.DESCRIPTION
  - Uses Microsoft Graph PowerShell (New-MgApplication) to create the registration.
  - Sets the custom scope (OAuth2PermissionScope) on the application – "chat".
  - Configures a required resource access entry for Microsoft Graph's User.Read scope.
  - Prints the resulting application info.
#>

param(
    [Parameter(Mandatory = $true)]
    [string]
    $DisplayName = "ChatWith-FrontEndApp",

    [Parameter(Mandatory = $true)]
    [string]
    $backendAppId = "`<your-backend-app-id>",

    [Parameter(Mandatory = $true)]
    [string]
    $backendScopeId = "<your-backend-scope-id>",

    [Parameter(Mandatory = $false)]
    [string]
    $backendUrl = "<your-backend-url>",

    [Parameter(Mandatory = $false)]
    [string]
    $TenantId = "<your-tenant-id>"
)

#  Ensure Microsoft Graph modules are available
#  You can comment out the install line if you already have them installed
if (-not (Get-Module Microsoft.Graph.Applications -ListAvailable)) {
    Write-Host "Installing Microsoft.Graph modules..."
    Install-Module Microsoft.Graph -Scope CurrentUser -Force
}

Import-Module Microsoft.Graph.Applications

# Connect to Microsoft Graph with appropriate app registration rights (Application.ReadWrite.All)
Write-Host "Connecting to Microsoft Graph..."
Connect-MgGraph -TenantId $TenantId -Scopes "Application.ReadWrite.All"
Select-MgProfile -Name "v1.0"

$backendUrl = "https://localhost:44321"
$backendAppId = "06ab7aa3-526b-4d77-8614-c1a03b50d53d"
$backendScopeId = "13a333a6-8df2-4c83-8b15-a91705b3d0af"

# Define required resource access for Microsoft Graph User.Read scope
# Microsoft Graph => resourceAppId = "00000003-0000-0000-c000-000000000000"
# The GUID for User.Read is "e1fe6dd8-ba31-4d61-89e7-88639da4683d"
$requiredAccessForGraph = @(
    @{
        resourceAppId  = "00000003-0000-0000-c000-000000000000"
        resourceAccess = @(
            @{
                id   = "7427e0e9-2fba-42fe-b0c0-848c9e6a8182"  # offline_access
                type = "Scope"
            },
            @{
                id   = "37f7f235-527c-4136-accd-4a02d197296e"  # openid
                type = "Scope"
            },
            @{
                id   = "e1fe6dd8-ba31-4d61-89e7-88639da4683d"  # User.Read
                type = "Scope"
            }
        )
    },
    @{
        resourceAppId  = $backendAppId
        resourceAccess = @(
            @{
                id   = $backendScopeId  # our backend scope
                type = "Scope"
            }
        )
    }
)

# Create the application using New-MgApplication
Write-Host "Creating the application registration..."
$newApp = New-MgApplication `
    -DisplayName $DisplayName `
    -SignInAudience "AzureADMyOrg" `
    -Spa @{ redirectUris = $backendUrl } `
    -RequiredResourceAccess $requiredAccessForGraph `
    -ServicePrincipalLockConfiguration @{
        AllProperties            = $true
        CredentialsWithUsageSign = $true
        CredentialsWithUsageVerify = $true
        IdentifierUris           = $false
        IsEnabled                = $true
        TokenEncryptionKeyId     = $true
    } `
    -Web @{
        homePageUrl = $null
        implicitGrantSettings = @{
            enableAccessTokenIssuance = $true
            enableIdTokenIssuance    = $false
        }
        logoutUrl           = $null
        redirectUriSettings = @()
        redirectUris        = @()
    }


Write-Host "`nApp Registration created successfully!"
Write-Host "  Display Name: " $newApp.DisplayName
Write-Host "  App Id:       " $newApp.AppId
Write-Host "  Object Id:    " $newApp.Id

Update-mgApplication -applicationId $appId `
-ServicePrincipalLockConfiguration @{
    AllProperties            = $true
    CredentialsWithUsageSign = $true
    CredentialsWithUsageVerify = $true
    IdentifierUris           = $false
    IsEnabled                = $true
    TokenEncryptionKeyId     = $true
} `
-Web @{
    implicitGrantSettings = @{
        enableAccessTokenIssuance = $true
        enableIdTokenIssuance    = $false
    }
}