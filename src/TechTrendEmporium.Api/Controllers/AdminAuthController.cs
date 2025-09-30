using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Logica.Interfaces;
using Data.Entities.Enums;
using Data.Entities;
using TechTrendEmporium.Api.DTOS; // si tus DTOs viven aquí; si no, quita este using

namespace TechTrendEmporium.Api.Controllers
{
    [ApiController]
    [Route("api/admin/auth")]
    public class AdminAuthController : ControllerBase
    {
        private readonly IUserService _userService;

        public AdminAuthController(IUserService userService)
        {
            _userService = userService;
        }

        /// <summary>
        /// Crea un usuario nuevo (solo Admin). 
        /// Requiere Bearer JWT con role=admin.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "admin")]  // AC#4 y AC#5
        [ProducesResponseType(typeof(UserResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(object), StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Register([FromBody] RegisterUserDto dto, CancellationToken cancellationToken)
        {
            // Validaciones básicas de entrada
            if (dto is null)
                return BadRequest(new { error = "Body required" });

            if (string.IsNullOrWhiteSpace(dto.Email) ||
                string.IsNullOrWhiteSpace(dto.Username) ||
                string.IsNullOrWhiteSpace(dto.Password) ||
                string.IsNullOrWhiteSpace(dto.Role))
            {
                return BadRequest(new { error = "email, username, password and role are required" });
            }

            // AC#1: email con formato válido
            if (!new EmailAddressAttribute().IsValid(dto.Email))
                return BadRequest(new { error = "Invalid email format" });

            // AC#3: solo se permite crear usuarios con rol employee
            if (!Enum.TryParse<Role>(dto.Role, true, out var parsedRole) || parsedRole != Role.Employee)
                return BadRequest(new { error = "Role must be 'employee'" });

            try
            {
                // Crea el usuario (el servicio valida unicidad de email/username → AC#1 y AC#2)
                var user = await _userService.CreateUserAsync(
                    dto.Email.Trim(),
                    dto.Username.Trim(),
                    dto.Password,   // asegúrate de hashear en el servicio
                    parsedRole,
                    cancellationToken
                );

                var response = new UserResponseDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    Username = user.Username,
                    Role = user.Role.ToString().ToLower()
                };

                // 201 Created con el recurso
                return CreatedAtAction(nameof(Register), new { id = user.Id }, response);
            }
            catch (InvalidOperationException ex)
            {
                // Si el servicio lanza por duplicados, puedes mapear a 409
                // Usa el mensaje para saber si fue email o username
                var message = ex.Message ?? "Conflict";
                var isDuplicate = message.Contains("email", StringComparison.OrdinalIgnoreCase) ||
                                  message.Contains("username", StringComparison.OrdinalIgnoreCase);
                if (isDuplicate)
                    return Conflict(new { error = message });   // AC#1 / AC#2

                return BadRequest(new { error = message });
            }
        }
    }

    // Si no tienes aún estos DTOs, déjalos aquí; si ya existen en TechTrendEmporium.Api.DTOS, elimina esta región.
    #region DTOs (solo si no existen en tu proyecto)
    public class RegisterUserDto
    {
        public string Email { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        // debe venir "employee"
        public string Role { get; set; } = "employee";
    }

    public class UserResponseDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = "employee";
    }
    #endregion
}
