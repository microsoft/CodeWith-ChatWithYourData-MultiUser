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
    $DisplayName,

    [Parameter(Mandatory = $false)]
    [string]
    $TenantId
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

# Define the custom OAuth2PermissionScope array.
# For a scope to be usable by both admins and end users, set 'type' to 'User' or 'AdminOrUser'.
# If you want strictly admin-only, use 'Admin'. For both admin and user consent, try 'AdminOrUser'.
$scopeId = [Guid]::NewGuid()    # Insert or generate your own GUID
$customScopes = @(
    @{
        AdminConsentDescription  = "Allows the app to call the chat backend"
        AdminConsentDisplayName  = "Chat"
        Id                       = $scopeId    # Insert or generate your own GUID
        IsEnabled                = $true
        # 'Type' can be "User" (user consent) or "Admin" (admin-only consent).
        # Some tenants also support "AdminOrUser" for broader scenarios.
        Type                     = "User"
        UserConsentDescription   = "Give the app access to chat features"
        UserConsentDisplayName   = "Chat"
        Value                    = "chat"
    }
)

# Define required resource access for Microsoft Graph User.Read scope
# Microsoft Graph => resourceAppId = "00000003-0000-0000-c000-000000000000"
# The GUID for User.Read is "e1fe6dd8-ba31-4d61-89e7-88639da4683d"
$requiredAccessForGraph = @(
    @{
        resourceAppId  = "00000003-0000-0000-c000-000000000000"
        resourceAccess = @(
            @{
                id   = "e1fe6dd8-ba31-4d61-89e7-88639da4683d"  # User.Read
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
    -Api @{
        AcceptMappedClaims      = $true    # or $false, depending on your scenario
        KnownClientApplications = @()      # add client app IDs here if you want bundled consent
        Oauth2PermissionScopes  = $customScopes
        # Any other IMicrosoftGraphApiApplication properties can be set here.
    } `
    -ServicePrincipalLockConfiguration @{
        AllProperties            = $true
        CredentialsWithUsageSign = $true
        CredentialsWithUsageVerify = $true
        IdentifierUris           = $false
        IsEnabled                = $true
        TokenEncryptionKeyId     = $true
    } `
    -RequiredResourceAccess $requiredAccessForGraph

# Update the application with the correct IdentifierUris
Write-Host "Updating the application with the correct IdentifierUris..."
$IdentifierUri = "api://$($newApp.AppId)"
Update-MgApplication -ApplicationId $newApp.Id `
    -IdentifierUris @($IdentifierUri) `
    


Write-Host "`nApp Registration created successfully!"
Write-Host "  Display Name: " $newApp.DisplayName
Write-Host "  App Id:       " $newApp.AppId
Write-Host "  Object Id:    " $newApp.Id
Write-Host "  Scope Id:    " $scopeId
Write-Host "  Custom Scope: " ($newApp.Api.oAuth2PermissionScopes | Select-Object -ExpandProperty value)
Write-Host "  Identifier Uris: " ($IdentifierUri)

# Create the service principal tied to the new application, this is needed to perform the authentication
# Otherwise we get an AADSTS650052: The app is trying to access a service '06ab7aa3-526b-4d77-8614-c1a03b50d53d'(CodeWithBackEndApp) that your organization 'f903e023-a92d-4561-9a3b-d8429e3fa1fd' lacks a service principal for
$sp = New-MgServicePrincipal -AppId $newApp.AppId

Write-Host "`nCreated service principal:"
Write-Host "  Display Name: " $sp.DisplayName
Write-Host "  App Id:       " $sp.AppId
Write-Host "  SP Object Id: " $sp.Id