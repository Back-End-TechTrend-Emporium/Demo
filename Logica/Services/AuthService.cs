using Data.Entities;
using Data.Entities.Enums;
using Logica.Interfaces;
using Logica.Models;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using System.IdentityModel.Tokens.Jwt;
using BCrypt.Net;

namespace Logica.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly ITokenService _tokenService;

        public AuthService(IUserRepository userRepository, ITokenService tokenService)
        {
            _userRepository = userRepository;
            _tokenService = tokenService;
        }

        public async Task<(AuthResponse? Response, string? Error)> RegisterShopperAsync(ShopperRegisterRequest request)
        {
            if (await _userRepository.EmailExistsAsync(request.Email) || await _userRepository.UsernameExistsAsync(request.Username))
            {
                return (null, "El email o nombre de usuario ya existe.");
            }

            var user = new User
            {
                Email = request.Email,
                Username = request.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Role = Role.Shopper
            };

            await _userRepository.AddAsync(user);

            var token = _tokenService.CreateToken(user);
            var response = new AuthResponse(user.Id, user.Email, user.Username, user.Role.ToString(), token);
            return (response, null);
        }

        public async Task<(AuthResponse? Response, string? Error)> RegisterByAdminAsync(AdminRegisterRequest request)
        {
            if (!Enum.TryParse<Role>(request.Role, true, out var role) || role != Role.Employee)
            {
                return (null, "El rol especificado es inválido. Solo se pueden crear empleados.");
            }

            if (await _userRepository.EmailExistsAsync(request.Email) || await _userRepository.UsernameExistsAsync(request.Username))
            {
                return (null, "El email o nombre de usuario ya existe.");
            }

            var user = new User
            {
                Email = request.Email,
                Username = request.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Role = role
            };

            await _userRepository.AddAsync(user);

            var token = _tokenService.CreateToken(user);
            var response = new AuthResponse(user.Id, user.Email, user.Username, user.Role.ToString(), token);
            return (response, null);
        }

        public async Task<(AuthResponse? Response, string? Error)> LoginAsync(LoginRequest request, string? ipAddress, string? userAgent)
        {
            var user = await _userRepository.GetByEmailAsync(request.Email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return (null, "Email o contraseña incorrectos.");
            }

            user.LastLoginAt = DateTime.UtcNow;
            await _userRepository.UpdateUserAsync(user);

            // Genera el token PRIMERO para poder obtener su JTI
            var tokenString = _tokenService.CreateToken(user);
            var jwtHandler = new JwtSecurityTokenHandler();
            var jwtToken = jwtHandler.ReadJwtToken(tokenString);
            var jti = jwtToken.Id;

            // Crea la nueva sesión con todos los datos
            var session = new Session
            {
                UserId = user.Id,
                Status = SessionStatus.Active,
                Ip = ipAddress,
                UserAgent = userAgent,
                TokenJtiHash = jti // Guardamos el JTI del token
            };
            await _userRepository.CreateSessionAsync(session);

            var response = new AuthResponse(user.Id, user.Email, user.Username, user.Role.ToString(), tokenString);
            return (response, null);
        }

        public async Task<(bool Success, string? Error)> LogoutAsync(ClaimsPrincipal userPrincipal)
        {
            var jtiClaim = userPrincipal.FindFirst(JwtRegisteredClaimNames.Jti);
            if (jtiClaim == null)
            {
                return (false, "Token inválido, no contiene JTI.");
            }

            var jti = jtiClaim.Value;
            var activeSession = await _userRepository.GetActiveSessionByJtiAsync(jti);

            if (activeSession == null)
            {
                return (false, "No se encontró una sesión activa para este token.");
            }

            activeSession.Status = SessionStatus.Closed;
            activeSession.ClosedAt = DateTime.UtcNow;
            await _userRepository.UpdateSessionAsync(activeSession);

            return (true, null);
        }
    }
}