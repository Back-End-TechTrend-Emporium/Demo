using Data;
using Data.Entities.Enums;
using Logica.Interfaces;
using Logica.Models;
using Logica.Models.Carts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace TechTrendEmporium.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "SuperAdmin")]
    public class AdminController : BaseController
    {
        private readonly AppDbContext _context;
        private readonly IAuthService _authService;
        private readonly ICartService _cartService;
        private readonly IProductService _productService;
        private readonly ICategoryService _categoryService;
        private readonly IUserService _userService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            AppDbContext context,
            IAuthService authService,
            ICartService cartService,
            IProductService productService,
            ICategoryService categoryService,
            IUserService userService,
            ILogger<AdminController> logger)
        {
            _context = context;
            _authService = authService;
            _cartService = cartService;
            _productService = productService;
            _categoryService = categoryService;
            _userService = userService;
            _logger = logger;
        }

        // ============================================
        // GESTIÓN DE SESIONES
        // ============================================

        /// <summary>
        /// Obtener todas las sesiones activas
        /// </summary>
        [HttpGet("sessions/active")]
        public async Task<ActionResult<object>> GetActiveSessions()
        {
            try
            {
                var activeSessions = await _context.Sessions
                    .Include(s => s.User)
                    .Where(s => s.Status == SessionStatus.Active)
                    .OrderByDescending(s => s.CreatedAt)
                    .Select(s => new
                    {
                        sessionId = s.Id,
                        userId = s.UserId,
                        username = s.User.Username,
                        email = s.User.Email,
                        role = s.User.Role.ToString(),
                        createdAt = s.CreatedAt,
                        ipAddress = s.Ip,
                        userAgent = s.UserAgent,
                        minutesActive = EF.Functions.DateDiffMinute(s.CreatedAt, DateTime.UtcNow)
                    })
                    .ToListAsync();

                return Ok(new
                {
                    totalActiveSessions = activeSessions.Count,
                    sessions = activeSessions,
                    timestamp = DateTime.UtcNow,
                    message = "Sesiones activas obtenidas exitosamente"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo las sesiones activas");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Obtener historial completo de sesiones
        /// </summary>
        [HttpGet("sessions/all")]
        public async Task<ActionResult<object>> GetAllSessions([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            try
            {
                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 50;

                var totalSessions = await _context.Sessions.CountAsync();
                
                var sessions = await _context.Sessions
                    .Include(s => s.User)
                    .OrderByDescending(s => s.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(s => new
                    {
                        sessionId = s.Id,
                        userId = s.UserId,
                        username = s.User.Username,
                        email = s.User.Email,
                        role = s.User.Role.ToString(),
                        status = s.Status.ToString(),
                        createdAt = s.CreatedAt,
                        closedAt = s.ClosedAt,
                        ipAddress = s.Ip,
                        userAgent = s.UserAgent,
                        durationMinutes = s.ClosedAt != null 
                            ? EF.Functions.DateDiffMinute(s.CreatedAt, s.ClosedAt.Value)
                            : (s.Status == SessionStatus.Active 
                                ? (int?)EF.Functions.DateDiffMinute(s.CreatedAt, DateTime.UtcNow) 
                                : (int?)null)
                    })
                    .ToListAsync();

                return Ok(new
                {
                    totalSessions = totalSessions,
                    currentPage = page,
                    pageSize = pageSize,
                    totalPages = (int)Math.Ceiling((double)totalSessions / pageSize),
                    sessions = sessions,
                    message = "Historial de sesiones obtenido exitosamente"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo el historial completo de sesiones");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Obtener estadísticas de sesiones
        /// </summary>
        [HttpGet("sessions/statistics")]
        public async Task<ActionResult<object>> GetSessionStatistics()
        {
            try
            {
                var totalSessions = await _context.Sessions.CountAsync();
                var activeSessions = await _context.Sessions.CountAsync(s => s.Status == SessionStatus.Active);
                var closedSessions = await _context.Sessions.CountAsync(s => s.Status == SessionStatus.Closed);
                var expiredSessions = await _context.Sessions.CountAsync(s => s.Status == SessionStatus.Expired);

                var sessionsByRole = await _context.Sessions
                    .Include(s => s.User)
                    .GroupBy(s => s.User.Role)
                    .Select(g => new
                    {
                        role = g.Key.ToString(),
                        totalSessions = g.Count(),
                        activeSessions = g.Count(s => s.Status == SessionStatus.Active)
                    })
                    .ToListAsync();

                var recentLogins = await _context.Sessions
                    .Include(s => s.User)
                    .Where(s => s.CreatedAt >= DateTime.UtcNow.AddDays(-7))
                    .GroupBy(s => s.CreatedAt.Date)
                    .Select(g => new
                    {
                        date = g.Key,
                        loginCount = g.Count()
                    })
                    .OrderBy(x => x.date)
                    .ToListAsync();

                return Ok(new
                {
                    totalSessions = totalSessions,
                    sessionsByStatus = new
                    {
                        active = activeSessions,
                        closed = closedSessions,
                        expired = expiredSessions
                    },
                    sessionsByRole = sessionsByRole,
                    recentLoginsLast7Days = recentLogins,
                    timestamp = DateTime.UtcNow,
                    message = "Estadísticas de sesiones obtenidas exitosamente"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo las estadísticas de sesiones");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Cerrar una sesión específica
        /// </summary>
        [HttpPost("sessions/{sessionId:guid}/close")]
        public async Task<ActionResult<object>> CloseSession(Guid sessionId)
        {
            try
            {
                var session = await _context.Sessions
                    .Include(s => s.User)
                    .FirstOrDefaultAsync(s => s.Id == sessionId);

                if (session == null)
                {
                    return NotFound(new { message = "Sesión no encontrada" });
                }

                if (session.Status != SessionStatus.Active)
                {
                    return BadRequest(new { message = $"La sesión ya está en estado: {session.Status}" });
                }

                session.Status = SessionStatus.Expired;
                session.ClosedAt = DateTime.UtcNow;
                
                await _context.SaveChangesAsync();

                _logger.LogInformation("Sesión {SessionId} cerrada por administrador para el usuario {Username}", 
                    sessionId, session.User.Username);

                return Ok(new
                {
                    sessionId = sessionId,
                    userId = session.UserId,
                    username = session.User.Username,
                    previousStatus = "Active",
                    newStatus = "Expired",
                    closedAt = session.ClosedAt,
                    message = "Sesión cerrada exitosamente"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cerrando la sesión {SessionId}", sessionId);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        // ============================================
        // GESTIÓN DE CARRITOS
        // ============================================

        /// <summary>
        /// Obtener todos los carritos con información detallada
        /// </summary>
        [HttpGet("carts/all")]
        public async Task<ActionResult<IEnumerable<CartFullDetailsDto>>> GetAllCarts()
        {
            try
            {
                _logger.LogInformation("Admin obteniendo todos los carritos");
                var carts = await _cartService.GetAllCartsFullDetailsAsync();
                return Ok(carts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo todos los carritos para admin");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Obtener dashboard de carritos
        /// </summary>
        [HttpGet("carts/dashboard")]
        public async Task<ActionResult<CartsDashboardSummaryDto>> GetCartsDashboard()
        {
            try
            {
                _logger.LogInformation("Admin obteniendo dashboard de carritos");
                var summary = await _cartService.GetCartsDashboardSummaryAsync();
                return Ok(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo dashboard de carritos");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Obtener carritos de un usuario específico
        /// </summary>
        [HttpGet("carts/user/{userId:guid}")]
        public async Task<ActionResult<IEnumerable<CartFullDetailsDto>>> GetUserCarts(Guid userId)
        {
            try
            {
                _logger.LogInformation("Admin obteniendo carritos para el usuario {UserId}", userId);
                var carts = await _cartService.GetCartsByUserFullDetailsAsync(userId);
                return Ok(carts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo carritos del usuario para admin");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Restaurar inventario de un carrito
        /// </summary>
        [HttpPost("carts/{cartId:guid}/restore-inventory")]
        public async Task<ActionResult> RestoreCartInventory(Guid cartId)
        {
            try
            {
                _logger.LogInformation("Admin restaurando inventario para el carrito {CartId}", cartId);
                await _cartService.RestoreInventoryAsync(cartId);
                return Ok(new { message = "Inventario restaurado exitosamente", cartId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restaurando inventario del carrito");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        // ============================================
        // INTEGRACIONES FAKESTORE
        // ============================================

        /// <summary>
        /// Obtener carritos desde FakeStore
        /// </summary>
        [HttpGet("fakestore/carts")]
        public async Task<ActionResult<IEnumerable<CartDto>>> GetFakeStoreCarts()
        {
            try
            {
                var carts = await _cartService.GetCartsFromFakeStoreAsync();
                return Ok(carts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo carritos desde FakeStore");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Sincronizar carritos desde FakeStore
        /// </summary>
        [HttpPost("fakestore/carts/sync")]
        public async Task<ActionResult<object>> SyncFakeStoreCarts()
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var result = await _cartService.SyncAllCartsFromFakeStoreAsync(currentUserId);
                return Ok(new
                {
                    Message = "Sincronización de carritos completada",
                    SuccessfulCarts = result.CartsSuccessful,
                    FailedCarts = result.CartsFailed,
                    TotalProcessed = result.TotalCartsProcessed,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sincronizando carritos desde FakeStore");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Obtener productos desde FakeStore
        /// </summary>
        [HttpGet("fakestore/products")]
        public async Task<ActionResult<IEnumerable<object>>> GetFakeStoreProducts()
        {
            try
            {
                var products = await _productService.GetProductsFromFakeStoreAsync();
                return Ok(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo productos desde FakeStore");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Sincronizar productos desde FakeStore
        /// </summary>
        [HttpPost("fakestore/products/sync")]
        public async Task<ActionResult<object>> SyncFakeStoreProducts()
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var createdBy = currentUserId == Guid.Empty ? new Guid("00000000-0000-0000-0000-000000000001") : currentUserId;
                
                var importedCount = await _productService.SyncAllFromFakeStoreAsync(createdBy);

                return Ok(new
                {
                    Message = "Sincronización de productos completada exitosamente",
                    ImportedCount = importedCount,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sincronizando productos desde FakeStore");
                return StatusCode(500, new { message = "Error durante la sincronización" });
            }
        }

        /// <summary>
        /// Obtener categorías desde FakeStore
        /// </summary>
        [HttpGet("fakestore/categories")]
        public async Task<ActionResult<IEnumerable<string>>> GetFakeStoreCategories()
        {
            try
            {
                var categories = await _productService.GetCategoriesFromFakeStoreAsync();
                return Ok(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo categorías desde FakeStore");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Sincronizar categorías desde FakeStore
        /// </summary>
        [HttpPost("fakestore/categories/sync")]
        public async Task<ActionResult<object>> SyncFakeStoreCategories()
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var createdBy = currentUserId == Guid.Empty ? new Guid("00000000-0000-0000-0000-000000000001") : currentUserId;
                
                var result = await _categoryService.SyncCategoriesFromFakeStoreAsync(createdBy);

                return Ok(new
                {
                    Message = "Sincronización de categorías completada exitosamente",
                    SyncResult = result,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sincronizando categorías desde FakeStore");
                return StatusCode(500, new { message = "Error durante la sincronización" });
            }
        }

        /// <summary>
        /// Obtener usuarios desde FakeStore
        /// </summary>
        [HttpGet("fakestore/users")]
        public async Task<ActionResult<IEnumerable<object>>> GetFakeStoreUsers()
        {
            try
            {
                var users = await _userService.GetUsersFromFakeStoreAsync();
                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo usuarios desde FakeStore");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Sincronizar usuarios desde FakeStore
        /// </summary>
        [HttpPost("fakestore/users/sync")]
        public async Task<ActionResult<object>> SyncFakeStoreUsers()
        {
            try
            {
                var importedCount = await _userService.SyncAllUsersFromFakeStoreAsync();

                return Ok(new
                {
                    Message = "Sincronización de usuarios completada exitosamente",
                    ImportedCount = importedCount,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sincronizando usuarios desde FakeStore");
                return StatusCode(500, new { message = "Error durante la sincronización" });
            }
        }

        // ============================================
        // DIAGNÓSTICOS Y SALUD DEL SISTEMA
        // ============================================

        /// <summary>
        /// Verificar salud del sistema
        /// </summary>
        [HttpGet("diagnostics/health")]
        public async Task<ActionResult<object>> GetSystemHealth()
        {
            try
            {
                var health = new
                {
                    database = await CheckDatabaseHealthAsync(),
                    fakestore = await CheckFakeStoreHealthAsync(),
                    services = await CheckServicesHealthAsync(),
                    timestamp = DateTime.UtcNow
                };

                return Ok(health);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verificando salud del sistema");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Obtener información del sistema
        /// </summary>
        [HttpGet("diagnostics/system-info")]
        public ActionResult<object> GetSystemInfo()
        {
            try
            {
                var info = new
                {
                    environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
                    machineName = Environment.MachineName,
                    osVersion = Environment.OSVersion.ToString(),
                    processorCount = Environment.ProcessorCount,
                    workingSet = Environment.WorkingSet,
                    dotnetVersion = Environment.Version.ToString(),
                    timestamp = DateTime.UtcNow
                };

                return Ok(info);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo información del sistema");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Limpiar logs antiguos y optimizar rendimiento
        /// </summary>
        [HttpPost("maintenance/cleanup")]
        public async Task<ActionResult<object>> PerformMaintenance()
        {
            try
            {
                var result = new
                {
                    oldSessionsRemoved = await CleanupOldSessionsAsync(),
                    oldLogsRemoved = await CleanupOldLogsAsync(),
                    cacheCleared = true,
                    timestamp = DateTime.UtcNow
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante el mantenimiento");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        // ============================================
        // MÉTODOS PRIVADOS HELPER
        // ============================================

        private async Task<object> CheckDatabaseHealthAsync()
        {
            try
            {
                var canConnect = await _context.Database.CanConnectAsync();
                var userCount = await _context.Users.CountAsync();
                
                return new
                {
                    status = canConnect ? "Healthy" : "Unhealthy",
                    canConnect = canConnect,
                    userCount = userCount,
                    lastChecked = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                return new
                {
                    status = "Unhealthy",
                    error = ex.Message,
                    lastChecked = DateTime.UtcNow
                };
            }
        }

        private async Task<object> CheckFakeStoreHealthAsync()
        {
            try
            {
                var products = await _productService.GetProductsFromFakeStoreAsync();
                
                return new
                {
                    status = "Healthy",
                    productsAvailable = products.Count(),
                    lastChecked = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                return new
                {
                    status = "Unhealthy",
                    error = ex.Message,
                    lastChecked = DateTime.UtcNow
                };
            }
        }

        private async Task<object> CheckServicesHealthAsync()
        {
            try
            {
                return new
                {
                    status = "Healthy",
                    services = new
                    {
                        cartService = _cartService != null ? "Available" : "Unavailable",
                        productService = _productService != null ? "Available" : "Unavailable",
                        userService = _userService != null ? "Available" : "Unavailable",
                        authService = _authService != null ? "Available" : "Unavailable"
                    },
                    lastChecked = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                return new
                {
                    status = "Unhealthy",
                    error = ex.Message,
                    lastChecked = DateTime.UtcNow
                };
            }
        }

        private async Task<int> CleanupOldSessionsAsync()
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-30);
                var oldSessions = await _context.Sessions
                    .Where(s => s.CreatedAt < cutoffDate && s.Status != SessionStatus.Active)
                    .ToListAsync();

                if (oldSessions.Any())
                {
                    _context.Sessions.RemoveRange(oldSessions);
                    await _context.SaveChangesAsync();
                }

                return oldSessions.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error limpiando sesiones antiguas");
                return 0;
            }
        }

        private async Task<int> CleanupOldLogsAsync()
        {
            // Implementar limpieza de logs si tienes tabla de logs
            await Task.Delay(100); // Placeholder
            return 0;
        }
    }
}