using Infrastructure.Interfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace WebApi.HealthChecks
{
    public class SearchServiceHealthCheck : IHealthCheck
    {
        private readonly ISearchService _searchService;

        public SearchServiceHealthCheck(ISearchService searchService)
        {
            _searchService = searchService;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                // Call a lightweight operation to verify search service is working
                await _searchService.IsHealthyAsync();
                return HealthCheckResult.Healthy("Search service is healthy.");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("Search service is unhealthy.", ex);
            }
        }
    }
    
}
