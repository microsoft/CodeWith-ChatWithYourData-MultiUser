using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace WebApi.HealthChecks
{
    public class CosmosHealthCheck : IHealthCheck
    {
        private readonly CosmosClient _cosmosClient;
        private readonly IConfiguration _configuration;

        public CosmosHealthCheck(CosmosClient cosmosClient, IConfiguration configuration)
        {
            _cosmosClient = cosmosClient;
            _configuration = configuration;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var databaseName = _configuration["Cosmos:DatabaseName"];
                var database = _cosmosClient.GetDatabase(databaseName);
                await database.ReadAsync(cancellationToken: cancellationToken);
                return HealthCheckResult.Healthy("Cosmos DB connection is healthy.");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("Cosmos DB connection is unhealthy.", ex);
            }
        }
    }
}
