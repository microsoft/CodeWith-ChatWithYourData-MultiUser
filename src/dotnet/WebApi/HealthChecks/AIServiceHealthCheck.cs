using Infrastructure.Interfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace WebApi.HealthChecks
{
    public class AIServiceHealthCheck : IHealthCheck
    {
        private readonly IAIService _aiService;

        public AIServiceHealthCheck(IAIService aiService)
        {
            _aiService = aiService;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                // Call a lightweight operation to verify AI service is working
                await _aiService.IsHealthyAsync();
                return HealthCheckResult.Healthy("AI service is healthy.");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("AI service is unhealthy.", ex);
            }
        }
    }
}
