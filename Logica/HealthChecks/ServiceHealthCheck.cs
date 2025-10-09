using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Logica.Interfaces;

namespace Logica.HealthChecks
{
    public class ServiceHealthCheck : IHealthCheck
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ServiceHealthCheck> _logger;

        public ServiceHealthCheck(IServiceProvider serviceProvider, ILogger<ServiceHealthCheck> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var data = new Dictionary<string, object>();
                var issues = new List<string>();

                // Check critical services
                CheckService<IProductService>("ProductService", data, issues);
                CheckService<IUserService>("UserService", data, issues);
                CheckService<ICartService>("CartService", data, issues);
                CheckService<IAuthService>("AuthService", data, issues);
                CheckService<ITokenService>("TokenService", data, issues);

                data["CheckedAt"] = DateTime.UtcNow;
                data["TotalServices"] = data.Count - 1; // Exclude CheckedAt

                if (issues.Any())
                {
                    data["Issues"] = issues;
                    return Task.FromResult(HealthCheckResult.Unhealthy("Some services are not properly registered", null, data));
                }

                return Task.FromResult(HealthCheckResult.Healthy("All critical services are registered", data));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Service health check failed");
                return Task.FromResult(HealthCheckResult.Unhealthy($"Service check error: {ex.Message}"));
            }
        }

        private void CheckService<T>(string serviceName, Dictionary<string, object> data, List<string> issues)
        {
            try
            {
                var service = _serviceProvider.GetService<T>();
                if (service != null)
                {
                    data[serviceName] = "OK";
                }
                else
                {
                    data[serviceName] = "NOT_REGISTERED";
                    issues.Add($"{serviceName} is not registered");
                }
            }
            catch (Exception ex)
            {
                data[serviceName] = $"ERROR: {ex.Message}";
                issues.Add($"{serviceName} registration error: {ex.Message}");
            }
        }
    }
}