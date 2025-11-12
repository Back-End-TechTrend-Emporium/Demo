using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace TechTrendEmporium.Api.Controllers
{
 
    public abstract class BaseController : ControllerBase
    {
        
        protected Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(userIdClaim, out var userId))
            {
                return userId;
            }
            return Guid.Empty;
        }

  
        protected string GetCurrentUserRole()
        {

            return User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
        }

     
        protected bool HasRole(string role)
        {
            return User.IsInRole(role);
        }

    
        protected bool IsAdmin()
        {
            return HasRole("SuperAdmin");
        }

        
    }
}