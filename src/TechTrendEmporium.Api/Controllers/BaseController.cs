using Microsoft.AspNetCore.Mvc;

namespace TechTrendEmporium.Api.Controllers
{
    /// <summary>
    /// Controlador base que contiene m�todos comunes para todos los controladores
    /// </summary>
    public abstract class BaseController : ControllerBase
    {
        #region Helper Methods

        /// <summary>
        /// Obtiene el ID del usuario actual desde el token JWT (temporal)
        /// En un sistema real, esto vendr�a del ClaimsPrincipal
        /// </summary>
        protected Guid GetCurrentUserId()
        {
            // Por ahora, devolver un GUID fijo para testing
            // En producci�n, esto ser�a algo como:
            // return Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());
            return Guid.Parse("00000000-0000-0000-0000-000000000001");
        }

        /// <summary>
        /// Obtiene el rol del usuario actual desde el token JWT (temporal)
        /// </summary>
        protected string GetCurrentUserRole()
        {
            // En producci�n, esto ser�a algo como:
            // return User.FindFirst(ClaimTypes.Role)?.Value ?? throw new UnauthorizedAccessException();
            return "Admin"; // Valor temporal para testing
        }

        /// <summary>
        /// Verifica si el usuario actual tiene un rol espec�fico
        /// </summary>
        protected bool HasRole(string role)
        {
            return GetCurrentUserRole().Equals(role, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Verifica si el usuario actual es administrador
        /// </summary>
        protected bool IsAdmin()
        {
            return HasRole("Admin");
        }

        #endregion
    }
}