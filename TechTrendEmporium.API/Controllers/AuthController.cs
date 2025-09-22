using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TechTrendEmporium.Core.DTOs;
using TechTrendEmporium.Core.Entities;

namespace TechTrendEmporium.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly IConfiguration _configuration;

    public AuthController(
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _configuration = configuration;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthenticationResult>> Register([FromBody] RegisterRequest request)
    {
        if (request.Password != request.ConfirmPassword)
        {
            return BadRequest(new AuthenticationResult(
                false, null, null, null, "Passwords do not match", null));
        }

        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return BadRequest(new AuthenticationResult(
                false, null, null, null, "User with this email already exists", null));
        }

        var user = new User
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return BadRequest(new AuthenticationResult(
                false, null, null, null, errors, null));
        }

        // Add default role
        await _userManager.AddToRoleAsync(user, "Shopper");

        var token = await GenerateJwtToken(user);
        var roles = await _userManager.GetRolesAsync(user);

        return Ok(new AuthenticationResult(
            true, token, user.Id, roles, null, DateTime.UtcNow.AddHours(24)));
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthenticationResult>> Login([FromBody] LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null || !await _userManager.CheckPasswordAsync(user, request.Password))
        {
            return Unauthorized(new AuthenticationResult(
                false, null, null, null, "Invalid email or password", null));
        }

        if (!user.IsActive)
        {
            return Unauthorized(new AuthenticationResult(
                false, null, null, null, "Account is deactivated", null));
        }

        // Update last login time
        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        var token = await GenerateJwtToken(user);
        var roles = await _userManager.GetRolesAsync(user);

        return Ok(new AuthenticationResult(
            true, token, user.Id, roles, null, DateTime.UtcNow.AddHours(24)));
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        // In a real application, you might want to blacklist the token
        // For now, we'll just return success since JWT tokens are stateless
        return Ok(new { message = "Logged out successfully" });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserDto>> GetCurrentUser()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
        {
            return Unauthorized();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        var roles = await _userManager.GetRolesAsync(user);

        var userDto = new UserDto(
            user.Id,
            user.Email!,
            user.FirstName,
            user.LastName,
            user.IsActive,
            user.CreatedAt,
            user.LastLoginAt,
            roles);

        return Ok(userDto);
    }

    [HttpPost("refresh")]
    [Authorize]
    public async Task<ActionResult<AuthenticationResult>> RefreshToken()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
        {
            return Unauthorized();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null || !user.IsActive)
        {
            return Unauthorized();
        }

        var token = await GenerateJwtToken(user);
        var roles = await _userManager.GetRolesAsync(user);

        return Ok(new AuthenticationResult(
            true, token, user.Id, roles, null, DateTime.UtcNow.AddHours(24)));
    }

    private async Task<string> GenerateJwtToken(User user)
    {
        var jwtKey = _configuration["Jwt:Key"] ?? "your-256-bit-secret-key-here-for-development-only";
        var jwtIssuer = _configuration["Jwt:Issuer"] ?? "TechTrendEmporium";
        var jwtAudience = _configuration["Jwt:Audience"] ?? "TechTrendEmporium";

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var roles = await _userManager.GetRolesAsync(user);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email!),
            new(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
            new("FirstName", user.FirstName),
            new("LastName", user.LastName)
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}