<#
.SYNOPSIS
    Assigns specified Azure roles to a managed identity and configures Cosmos DB role assignment.

.DESCRIPTION
    This script:
      1) Uses the Azure CLI to assign multiple roles (like OpenAI User, Storage Blob roles, Search Index roles) to a managed identity.
      2) Configures a Cosmos DB SQL role assignment for the same principal, granting required permissions at account level.
#>

# Resource group containing the resources
$resourceGroup = 'rg-codewith-rag-2'

# Managed identity's object ID in Azure AD
$principalId = 'b8450f9c-9e79-4dbb-8d29-c2c34d7537d8'

# Subscription under which all resources belong
$subscriptionId = '5b398137-467e-43bb-9c4b-a9de3fcb2c37'

# Cosmos DB account name
$cosmosDb = 'cosmos-5zdkf6tjrgugk'

# List of Azure role IDs to be assigned
# Each role corresponds to a specific service capability (OpenAI, Storage, Search, etc.)
$roles = @(
    "5e0bd9bd-7b93-4f28-af87-19fc36ad61bd" # Cognitive Services OpenAI User
    "2a2b9908-6ea1-4ae2-8e65-a410df84e7d1" # Storage Blob Data Reader
    "ba92f5b4-2d11-453d-a403-e96b0029c9fe" # Storage Blob Data Contributor
    "1407120a-92aa-4202-b7e9-c0e197c71c8f" # Search Index Data Reader
    "8ebe5a00-799e-43f5-93ac-243d3dce84a7" # Search Index Data Contributor
)

# Loop through the roles, assigning each at the resource group scope
foreach ($role in $roles) {
    az role assignment create `
        --role $role `
        --assignee-object-id $principalId  `
        --scope "/subscriptions/$subscriptionId/resourceGroups/$resourceGroup" `
        --assignee-principal-type User
}

# Construct the Cosmos DB scope path
$cosmosScope = "/subscriptions/$subscriptionId/resourceGroups/$resourceGroup/providers/Microsoft.DocumentDB/databaseAccounts/$cosmosDb"

# Parameters to create a Cosmos DB SQL role assignment
$parameters = @{
    ResourceGroupName = $resourceGroup
    AccountName       = $cosmosDb
    RoleDefinitionId  = "00000000-0000-0000-0000-000000000002" # Built-in Cosmos DB role
    PrincipalId       = $principalId
    Scope             = $cosmosScope
}

# Create the Cosmos DB role assignment
New-AzCosmosDBSqlRoleAssignment @parameters