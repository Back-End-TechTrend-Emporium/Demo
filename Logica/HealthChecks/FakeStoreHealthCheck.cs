using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using External.FakeStore;

namespace Logica.HealthChecks
{
    public class FakeStoreHealthCheck : IHealthCheck
    {
        private readonly IFakeStoreApiService _fakeStoreService;
        private readonly ILogger<FakeStoreHealthCheck> _logger;

        public FakeStoreHealthCheck(IFakeStoreApiService fakeStoreService, ILogger<FakeStoreHealthCheck> logger)
        {
            _fakeStoreService = fakeStoreService;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(10)); // 10 second timeout

                var products = await _fakeStoreService.GetProductsAsync();
                
                if (products?.Any() == true)
                {
                    var data = new Dictionary<string, object>
                    {
                        ["ProductCount"] = products.Count(),
                        ["LastChecked"] = DateTime.UtcNow,
                        ["ResponseTime"] = "< 10s"
                    };
                    
                    // 1. HEALTHY - Todo funciona correctamente
                    return HealthCheckResult.Healthy("FakeStore API is accessible", data);
                }
                
                // 2. DEGRADED - Funciona pero con problemas menores
                return HealthCheckResult.Degraded("FakeStore API returned empty response");
            }
            catch (TaskCanceledException)
            {
                _logger.LogWarning("FakeStore health check timed out");
                // 3. UNHEALTHY - No funciona o falla crítico
                return HealthCheckResult.Unhealthy("FakeStore API timeout");
            }
            catch (Exception ex)    
            {
                _logger.LogError(ex, "FakeStore health check failed");
                // 3. UNHEALTHY - No funciona o falla crítico
                return HealthCheckResult.Unhealthy($"FakeStore API error: {ex.Message}");
            }
        }
    }
}