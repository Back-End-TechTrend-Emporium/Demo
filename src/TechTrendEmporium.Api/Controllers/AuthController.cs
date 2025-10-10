using Logica.Interfaces;
using Logica.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace TechTrendEmporium.Api.Controllers
{
    [ApiController]
    [Route("api")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IConfiguration _configuration; //IA debug de token

        public AuthController(IAuthService authService, IConfiguration configuration) //IA debug de token
        {
            _authService = authService;
            _configuration = configuration; //IA debug de token
        }

        //IA debug de token - Endpoint temporal para diagnosticar JWT (REMOVER EN PRODUCCIÓN)
        [HttpGet("debug/jwt")]
        public IActionResult DebugJwt()
        {
            //IA debug de token - Buscar la clave JWT en múltiples ubicaciones para compatibilidad con Azure
            var jwtKey = _configuration["Jwt:Key"] 
                      ?? _configuration["Jwt_Key"] 
                      ?? Environment.GetEnvironmentVariable("Jwt_Key")
                      ?? Environment.GetEnvironmentVariable("Jwt__Key");

            //IA debug de token - Retornar información de diagnóstico
            return Ok(new 
            { 
                jwtKeyFound = !string.IsNullOrWhiteSpace(jwtKey),
                jwtKeyLength = jwtKey?.Length ?? 0,
                environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
                siteName = Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME")
            });
        }
        //IA debug de token - FIN del endpoint de diagnóstico

        // F02: Sign Up Shopper
        [HttpPost("auth")]
        public async Task<IActionResult> RegisterShopper(ShopperRegisterRequest request)
        {
            var (response, error) = await _authService.RegisterShopperAsync(request);
            if (error != null) return BadRequest(new { message = error });

            return Ok(new { id = response!.Id, email = response.Email, username = response.Username });
        }

        // F01: Sign Up Admin (crea un empleado)
        [HttpPost("admin/auth")]
        [Authorize(Roles = "Admin, SuperAdmin")]
        public async Task<IActionResult> RegisterByAdmin(AdminRegisterRequest request)
        {
            var (response, error) = await _authService.RegisterByAdminAsync(request);
            if (error != null) return BadRequest(new { message = error });

            return Ok(new { id = response!.Id, email = response.Email, username = response.Username, role = response.Role });
        }

        // F03: Login User
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            // Capturamos los datos de la petición
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = Request.Headers["User-Agent"].ToString();

            // Los pasamos al servicio
            var (response, error) = await _authService.LoginAsync(request, ipAddress, userAgent);

            if (error != null) return Unauthorized(new { message = error });

            return Ok(new { token = response!.Token, email = response.Email, username = response.Username });
        }

        // F04: Logout User
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var (success, error) = await _authService.LogoutAsync(User);
            if (!success) return BadRequest(new { message = error });

            return Ok(new { status = "OK" });
        }
    }
}