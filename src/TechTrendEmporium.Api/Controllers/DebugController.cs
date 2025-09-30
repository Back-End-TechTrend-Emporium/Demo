using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TechTrendEmporium.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DebugController : ControllerBase
    {
        // Requiere JWT válido (cualquier rol)
        [HttpGet("whoami")]
        [Authorize]
        public IActionResult WhoAmI()
        {
            var identity = User.Identity?.Name ?? "(no name)";
            var claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();
            var role = User.Claims.FirstOrDefault(c => c.Type == "role")?.Value ?? "(no role claim)";
            return Ok(new { identity, role, claims });
        }
    }
}
