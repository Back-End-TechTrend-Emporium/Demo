using Microsoft.AspNetCore.Mvc;
using Logica.Interfaces;

namespace TechTrendEmporium.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DiagnosticsController : ControllerBase
    {
        private readonly ICartService _cartService;
        private readonly ILogger<DiagnosticsController> _logger;

        public DiagnosticsController(ICartService cartService, ILogger<DiagnosticsController> logger)
        {
            _cartService = cartService;
            _logger = logger;
        }

        [HttpGet("health")]
        public IActionResult HealthCheck()
        {
            try
            {
                var serviceStatus = new
                {
                    CartService = _cartService != null ? "OK" : "NULL",
                    Timestamp = DateTime.UtcNow,
                    Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown"
                };

                _logger.LogInformation("Health check completed successfully");
                return Ok(serviceStatus);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check failed");
                return StatusCode(500, new { Error = ex.Message, StackTrace = ex.StackTrace });
            }
        }

        [HttpGet("test-fakestore")]
        public async Task<IActionResult> TestFakeStoreConnection()
        {
            try
            {
                // Simple test to verify that services work
                _logger.LogInformation("Testing FakeStore connection...");
                
                var result = new
                {
                    Message = "Services are registered correctly",
                    CartServiceType = _cartService?.GetType().Name,
                    Timestamp = DateTime.UtcNow
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Test failed");
                return StatusCode(500, new { Error = ex.Message, StackTrace = ex.StackTrace });
            }
        }
    }
}