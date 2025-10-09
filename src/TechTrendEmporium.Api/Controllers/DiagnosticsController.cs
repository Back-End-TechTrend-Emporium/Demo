using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Logica.Interfaces;
using Data;
using Microsoft.EntityFrameworkCore;
using External.FakeStore;
using System.Reflection;
using System.Diagnostics;

namespace TechTrendEmporium.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DiagnosticsController : ControllerBase
    {
        private readonly HealthCheckService _healthCheckService;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DiagnosticsController> _logger;
        private readonly IConfiguration _configuration;

        public DiagnosticsController(
            HealthCheckService healthCheckService,
            IServiceProvider serviceProvider,
            ILogger<DiagnosticsController> logger,
            IConfiguration configuration)
        {
            _healthCheckService = healthCheckService;
            _serviceProvider = serviceProvider;
            _logger = logger;
            _configuration = configuration;
        }

        /// <summary>
        /// Comprehensive health check endpoint
        /// </summary>
        [HttpGet("health")]
        public async Task<IActionResult> HealthCheck()
        {
            try
            {
                var healthReport = await _healthCheckService.CheckHealthAsync();
                
                var response = new
                {
                    Status = healthReport.Status.ToString(),
                    TotalDuration = healthReport.TotalDuration.TotalMilliseconds,
                    Results = healthReport.Entries.ToDictionary(
                        kvp => kvp.Key,
                        kvp => new
                        {
                            Status = kvp.Value.Status.ToString(),
                            Description = kvp.Value.Description,
                            Duration = kvp.Value.Duration.TotalMilliseconds,
                            Data = kvp.Value.Data,
                            Exception = kvp.Value.Exception?.Message,
                            Tags = kvp.Value.Tags
                        }
                    ),
                    Timestamp = DateTime.UtcNow
                };

                var statusCode = healthReport.Status switch
                {
                    HealthStatus.Healthy => 200,
                    HealthStatus.Degraded => 200,
                    HealthStatus.Unhealthy => 503,
                    _ => 500
                };

                _logger.LogInformation("Health check completed with status: {Status}", healthReport.Status);
                return StatusCode(statusCode, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check failed");
                return StatusCode(500, new { Error = "Health check failed", Message = ex.Message });
            }
        }

        /// <summary>
        /// Quick liveness probe for container orchestration
        /// </summary>
        [HttpGet("live")]
        public IActionResult LivenessProbe()
        {
            return Ok(new { Status = "Alive", Timestamp = DateTime.UtcNow });
        }

        /// <summary>
        /// Readiness probe checking if the app is ready to serve traffic
        /// </summary>
        [HttpGet("ready")]
        public async Task<IActionResult> ReadinessProbe()
        {
            try
            {
                var healthReport = await _healthCheckService.CheckHealthAsync(
                    check => check.Tags.Contains("ready"));

                if (healthReport.Status == HealthStatus.Healthy)
                {
                    return Ok(new { Status = "Ready", Timestamp = DateTime.UtcNow });
                }

                return StatusCode(503, new 
                { 
                    Status = "NotReady", 
                    Issues = healthReport.Entries
                        .Where(e => e.Value.Status != HealthStatus.Healthy)
                        .Select(e => new { Service = e.Key, Status = e.Value.Status.ToString() }),
                    Timestamp = DateTime.UtcNow 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Readiness check failed");
                return StatusCode(503, new { Status = "NotReady", Error = ex.Message });
            }
        }

        /// <summary>
        /// Detailed system diagnostics
        /// </summary>
        [HttpGet("system")]
        public IActionResult SystemDiagnostics()
        {
            try
            {
                var process = Process.GetCurrentProcess();
                var assembly = Assembly.GetExecutingAssembly();

                var diagnostics = new
                {
                    Application = new
                    {
                        Name = assembly.GetName().Name,
                        Version = assembly.GetName().Version?.ToString(),
                        Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
                        MachineName = Environment.MachineName,
                        OSVersion = Environment.OSVersion.ToString(),
                        ProcessorCount = Environment.ProcessorCount,
                        Is64BitProcess = Environment.Is64BitProcess,
                        WorkingDirectory = Environment.CurrentDirectory
                    },
                    Performance = new
                    {
                        WorkingSet = GC.GetTotalMemory(false),
                        WorkingSetMB = Math.Round(GC.GetTotalMemory(false) / 1024.0 / 1024.0, 2),
                        ProcessorTime = process.TotalProcessorTime.TotalMilliseconds,
                        StartTime = process.StartTime,
                        Uptime = DateTime.Now - process.StartTime
                    },
                    Runtime = new
                    {
                        FrameworkDescription = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
                        OSDescription = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
                        ProcessArchitecture = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString(),
                        OSArchitecture = System.Runtime.InteropServices.RuntimeInformation.OSArchitecture.ToString()
                    },
                    Configuration = new
                    {
                        SwaggerEnabled = _configuration.GetValue<bool>("Swagger:Enabled", false),
                        DatabaseMigrationsOnStartup = _configuration.GetValue<bool>("EF:ApplyMigrationsOnStartup", false),
                        JwtConfigured = !string.IsNullOrEmpty(_configuration["Jwt:Key"]),
                        FakeStoreBaseUrl = _configuration["FakeStoreApi:BaseUrl"]
                    },
                    Timestamp = DateTime.UtcNow
                };

                return Ok(diagnostics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "System diagnostics failed");
                return StatusCode(500, new { Error = "System diagnostics failed", Message = ex.Message });
            }
        }

        /// <summary>
        /// Database connectivity and statistics
        /// </summary>
        [HttpGet("database")]
        public async Task<IActionResult> DatabaseDiagnostics()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var connectionTest = await context.Database.CanConnectAsync();
                
                if (!connectionTest)
                {
                    return StatusCode(503, new { Status = "DatabaseUnavailable", Timestamp = DateTime.UtcNow });
                }

                // Get connection info safely
                var connection = context.Database.GetDbConnection();
                var connectionString = context.Database.GetConnectionString();

                var stats = new
                {
                    Connection = new
                    {
                        CanConnect = connectionTest,
                        DatabaseName = connection.Database ?? "Unknown",
                        ConnectionString = MaskConnectionString(connectionString),
                        State = connection.State.ToString()
                    },
                    Statistics = new
                    {
                        UsersCount = await context.Users.CountAsync(),
                        ProductsCount = await context.Products.CountAsync(),
                        CategoriesCount = await context.Categories.CountAsync(),
                        CartsCount = await context.Carts.CountAsync(),
                        ReviewsCount = await context.Reviews.CountAsync()
                    },
                    PendingApprovals = new
                    {
                        Products = await context.Products.CountAsync(p => p.State == Data.Entities.Enums.ApprovalState.PendingApproval),
                        Categories = await context.Categories.CountAsync(c => c.State == Data.Entities.Enums.ApprovalState.PendingApproval)
                    },
                    Timestamp = DateTime.UtcNow
                };

                // Try to get server version safely
                try
                {
                    if (connection.State == System.Data.ConnectionState.Closed)
                    {
                        await connection.OpenAsync();
                    }
                    var serverVersion = connection.ServerVersion ?? "Unknown";
                    
                    // Add server version to connection info
                    var connectionWithVersion = new
                    {
                        stats.Connection.CanConnect,
                        stats.Connection.DatabaseName,
                        ServerVersion = serverVersion,
                        stats.Connection.ConnectionString,
                        stats.Connection.State
                    };

                    return Ok(new
                    {
                        Connection = connectionWithVersion,
                        stats.Statistics,
                        stats.PendingApprovals,
                        stats.Timestamp
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not retrieve server version");
                    return Ok(stats); // Return without server version
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database diagnostics failed");
                return StatusCode(500, new { Error = "Database diagnostics failed", Message = ex.Message });
            }
        }

        /// <summary>
        /// External services connectivity test
        /// </summary>
        [HttpGet("external-services")]
        public async Task<IActionResult> ExternalServicesDiagnostics()
        {
            try
            {
                var results = new Dictionary<string, object>();

                // Test FakeStore API
                try
                {
                    var fakeStoreService = _serviceProvider.GetRequiredService<IFakeStoreApiService>();
                    var stopwatch = Stopwatch.StartNew();
                    
                    var products = await fakeStoreService.GetProductsAsync();
                    stopwatch.Stop();

                    results["FakeStore"] = new
                    {
                        Status = "Healthy",
                        ResponseTime = stopwatch.ElapsedMilliseconds,
                        ProductCount = products?.Count() ?? 0,
                        BaseUrl = _configuration["FakeStoreApi:BaseUrl"]
                    };
                }
                catch (Exception ex)
                {
                    results["FakeStore"] = new
                    {
                        Status = "Unhealthy",
                        Error = ex.Message,
                        BaseUrl = _configuration["FakeStoreApi:BaseUrl"]
                    };
                }

                results["Timestamp"] = DateTime.UtcNow;
                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "External services diagnostics failed");
                return StatusCode(500, new { Error = "External services diagnostics failed", Message = ex.Message });
            }
        }

        /// <summary>
        /// Service registrations diagnostics
        /// </summary>
        [HttpGet("services")]
        public IActionResult ServicesDiagnostics()
        {
            try
            {
                var services = new Dictionary<string, object>();

                // Check critical service registrations
                CheckServiceRegistration<IProductService>("ProductService", services);
                CheckServiceRegistration<IUserService>("UserService", services);
                CheckServiceRegistration<ICartService>("CartService", services);
                CheckServiceRegistration<IAuthService>("AuthService", services);
                CheckServiceRegistration<ITokenService>("TokenService", services);
                CheckServiceRegistration<ICategoryService>("CategoryService", services);
                CheckServiceRegistration<IWishlistService>("WishlistService", services);
                CheckServiceRegistration<IReviewService>("ReviewService", services);
                CheckServiceRegistration<IFakeStoreApiService>("FakeStoreApiService", services);
                CheckServiceRegistration<AppDbContext>("AppDbContext", services);

                services["Timestamp"] = DateTime.UtcNow;
                services["TotalServices"] = services.Count - 1;

                return Ok(services);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Services diagnostics failed");
                return StatusCode(500, new { Error = "Services diagnostics failed", Message = ex.Message });
            }
        }

        #region Helper Methods

        private void CheckServiceRegistration<T>(string serviceName, Dictionary<string, object> results)
        {
            try
            {
                var service = _serviceProvider.GetService<T>();
                results[serviceName] = new
                {
                    Status = service != null ? "Registered" : "NotRegistered",
                    Type = service?.GetType().Name ?? "N/A"
                };
            }
            catch (Exception ex)
            {
                results[serviceName] = new
                {
                    Status = "Error",
                    Error = ex.Message
                };
            }
        }

        private static string MaskConnectionString(string? connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
                return "Not configured";

            // Mask password and sensitive data
            return System.Text.RegularExpressions.Regex.Replace(
                connectionString,
                @"(Password|Pwd)=([^;]*)",
                "$1=***",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        #endregion
    }
}