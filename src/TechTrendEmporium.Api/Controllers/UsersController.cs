using Logica.Interfaces;
using Logica.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TechTrendEmporium.Api.Controllers;

[ApiController]
[Route("api/user")]
// Protegemos todo el controlador para que solo Administradores y SuperAdmins puedan acceder a la lista.
[Authorize(Roles = "Administrator, SuperAdmin")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// Obtiene una lista de todos los usuarios.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllUsers()
    {
        var users = await _userService.GetAllUsersAsync();
        return Ok(users);
    }

    /// <summary>
    /// Crea un nuevo usuario. Solo accesible para SuperAdmin.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "SuperAdmin")] // Sobrescribe la autorización para ser más restrictivo.
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        var (user, error) = await _userService.CreateUserAsync(request);
        if (error != null) return BadRequest(new { message = error });

        // Devuelve el usuario creado y un link para acceder a él.
        return CreatedAtAction(nameof(GetAllUsers), new { id = user!.Id }, user);
    }

    /// <summary>
    /// Actualiza un usuario existente por su nombre de usuario. Solo accesible para SuperAdmin.
    /// </summary>
    [HttpPut("{username}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> UpdateUser(string username, [FromBody] UpdateUserRequest request)
    {
        var (user, error) = await _userService.UpdateUserAsync(username, request);
        if (error != null) return NotFound(new { message = error });

        return Ok(user);
    }

    /// <summary>
    /// Elimina uno o más usuarios por sus nombres de usuario. Solo accesible para SuperAdmin.
    /// </summary>
    [HttpDelete]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> DeleteUsers([FromBody] DeleteUsersRequest request)
    {
        var (success, error) = await _userService.DeleteUsersAsync(request);
        if (!success) return BadRequest(new { message = error });

        return NoContent(); // 204 No Content es una respuesta estándar para un DELETE exitoso.
    }
}