using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Data;
using Microsoft.EntityFrameworkCore;

namespace Logica.HealthChecks
{
    public class DatabaseHealthCheck : IHealthCheck
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DatabaseHealthCheck> _logger;

        public DatabaseHealthCheck(IServiceProvider serviceProvider, ILogger<DatabaseHealthCheck> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                
                var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);
                
                if (canConnect)
                {
                    // Get connection info safely
                    var connection = dbContext.Database.GetDbConnection();
                    
                    var data = new Dictionary<string, object>
                    {
                        ["Database"] = connection.Database ?? "Unknown",
                        ["ConnectionState"] = connection.State.ToString(),
                        ["CheckedAt"] = DateTime.UtcNow
                    };

                    // Try to get server version safely
                    try
                    {
                        if (connection.State == System.Data.ConnectionState.Closed)
                        {
                            await connection.OpenAsync(cancellationToken);
                        }
                        data["ServerVersion"] = connection.ServerVersion ?? "Unknown";
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Could not retrieve server version");
                        data["ServerVersion"] = "Could not retrieve";
                    }
                    finally
                    {
                        if (connection.State == System.Data.ConnectionState.Open)
                        {
                            await connection.CloseAsync();
                        }
                    }

                    return HealthCheckResult.Healthy("Database connection successful", data);
                }
                else
                {
                    return HealthCheckResult.Unhealthy("Cannot connect to database");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database health check failed");
                return HealthCheckResult.Unhealthy($"Database error: {ex.Message}");
            }
        }
    }
}