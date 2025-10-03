using Microsoft.AspNetCore.Mvc;

namespace TechTrendEmporium.Api.Controllers
{
 
    public abstract class BaseController : ControllerBase
    {
        
        protected Guid GetCurrentUserId()
        {
            
            return Guid.Parse("00000000-0000-0000-0000-000000000001");
        }

  
        protected string GetCurrentUserRole()
        {
            
            return "SuperAdmin"; 
        }

     
        protected bool HasRole(string role)
        {
            return GetCurrentUserRole().Equals(role, StringComparison.OrdinalIgnoreCase);
        }

    
        protected bool IsAdmin()
        {
            return HasRole("Admin");
        }

        
    }
}