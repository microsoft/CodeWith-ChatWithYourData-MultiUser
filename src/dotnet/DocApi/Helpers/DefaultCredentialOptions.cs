using Azure.Identity;

namespace DocApi.Helpers
{
    public static class DefaultCredentialOptions
    {
        private static DefaultAzureCredentialOptions GetDefaultAzureCredentialOptionsImpl(string? clientId, string? tenantId, IWebHostEnvironment environment)
        {

            DefaultAzureCredentialOptions credentialOptions = new()
            {
                Diagnostics =
            {
                LoggedHeaderNames = { "x-ms-request-id" },
                LoggedQueryParameters = { "api-version" },
                IsLoggingContentEnabled = true
            },

                //exclude all credential types for local development
                ExcludeSharedTokenCacheCredential = true,
                ExcludeVisualStudioCodeCredential = true,
                ExcludeVisualStudioCredential = true,
                ExcludeAzureCliCredential = true,
                ExcludeInteractiveBrowserCredential = true,
                ExcludeEnvironmentCredential = true,
                ExcludeAzureDeveloperCliCredential = true,
                ExcludeAzurePowerShellCredential = true,
                //include all credential types for production
                ExcludeWorkloadIdentityCredential = false,
                ExcludeManagedIdentityCredential = false

            };

            if (clientId is not null)
            {
                credentialOptions.ManagedIdentityClientId = clientId;
                credentialOptions.WorkloadIdentityClientId = clientId;
            }

            if (tenantId is not null)
            {
                credentialOptions.TenantId = tenantId;
            }


            if (environment.EnvironmentName == "Development")
            {
                credentialOptions.ExcludeManagedIdentityCredential = true;
                credentialOptions.ExcludeWorkloadIdentityCredential = true;
                //default to AzureCli or VSCredentials for local development
                credentialOptions.ExcludeAzureCliCredential = false;
                credentialOptions.ExcludeVisualStudioCredential = false;
            }

            return credentialOptions;
        }

        public static DefaultAzureCredentialOptions GetDefaultAzureCredentialOptions(IWebHostEnvironment environment)
        {
            return GetDefaultAzureCredentialOptionsImpl(null, null, environment);
        }

        public static DefaultAzureCredentialOptions GetDefaultAzureCredentialOptions(string clientId, IWebHostEnvironment environment)
        {
            return GetDefaultAzureCredentialOptionsImpl(clientId, null, environment);
        }

        public static DefaultAzureCredentialOptions GetDefaultAzureCredentialOptions(string clientId, string tenantId, IWebHostEnvironment environment)
        {
            return GetDefaultAzureCredentialOptionsImpl(clientId, tenantId, environment);
        }
    }
}
