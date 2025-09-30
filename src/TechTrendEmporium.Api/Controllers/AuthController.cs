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

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        // F02: Sign Up Shopper
        [HttpPost("auth")]
        public async Task<IActionResult> RegisterShopper(ShopperRegisterRequest request)
        {
            var (response, error) = await _authService.RegisterShopperAsync(request);
            if (error != null) return BadRequest(new { message = error });

            return Ok(new { id = response!.Id, email = response.Email, username = response.Username });
        }

        // F01: Sign Up Administrator (crea un empleado)
        [HttpPost("admin/auth")]
        [Authorize(Roles = "Administrator, SuperAdmin")]
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