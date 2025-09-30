using System;

namespace Logica.Models
{
    public record AuthResponse(
        Guid Id,
        string Email,
        string Username,
        string Role,
        string Token
    );
}