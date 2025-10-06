using System.ComponentModel.DataAnnotations;

namespace Logica.Models
{
    public record LoginRequest(
        [Required][EmailAddress] string Email,
        [Required] string Password
    );
}