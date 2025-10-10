using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Data.Entities;
using Data.Entities.Enums;
using Logica.Interfaces;
using Logica.Models;
using Logica.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using System.IdentityModel.Tokens.Jwt;
using System.Threading;

namespace TechTrendEmporium.Api.Tests.Services;

public class AuthServiceTests
{
    // Mocks for the service dependencies
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<ITokenService> _mockTokenService;
    private readonly Mock<ILogger<AuthService>> _mockLogger;

    // The service instance to be tested
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockTokenService = new Mock<ITokenService>();
        _mockLogger = new Mock<ILogger<AuthService>>();

        _authService = new AuthService(
            _mockUserRepository.Object,
            _mockTokenService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task RegisterShopperAsync_ShouldReturnResponse_WhenUserIsNew()
    {
        // Arrange
        var request = new ShopperRegisterRequest("new@example.com", "newuser", "Password123!");
        // Create a dummy user entity to be returned by the mock
        var userEntity = new User { Id = Guid.NewGuid(), Name = request.Username, Email = request.Email, Username = request.Username, Role = Role.Shopper };

        // Simulate that the user does not exist yet
        _mockUserRepository.Setup(repo => repo.EmailExistsAsync(request.Email, default)).ReturnsAsync(false);
        _mockUserRepository.Setup(repo => repo.UsernameExistsAsync(request.Username, default)).ReturnsAsync(false);

        // Setup the mock to return a user object when AddAsync is called.
        _mockUserRepository.Setup(repo => repo.AddAsync(It.IsAny<User>(), default)).ReturnsAsync(userEntity);

        _mockTokenService.Setup(ts => ts.CreateToken(It.IsAny<User>())).Returns("fake-jwt-token");

        // Act
        var (response, error) = await _authService.RegisterShopperAsync(request);

        // Assert
        Assert.Null(error); // This will now pass.
        Assert.NotNull(response);
        Assert.Equal(request.Email, response.Email);
        Assert.Equal("fake-jwt-token", response.Token);
        _mockUserRepository.Verify(repo => repo.AddAsync(It.Is<User>(u => u.Role == Role.Shopper), default), Times.Once);
    }

    [Fact]
    public async Task RegisterShopperAsync_ShouldReturnError_WhenUserExists()
    {
        // Arrange
        var request = new ShopperRegisterRequest("existing@example.com", "existinguser", "Password123!");

        _mockUserRepository.Setup(repo => repo.EmailExistsAsync(request.Email, default)).ReturnsAsync(true);

        // Act
        var (response, error) = await _authService.RegisterShopperAsync(request);

        // Assert
        Assert.Null(response);
        Assert.NotNull(error);
        Assert.Equal("El email o nombre de usuario ya existe.", error);
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnResponse_WhenCredentialsAreValid()
    {
        // Arrange
        var testPassword = "Password123!";
        var request = new LoginRequest("test@example.com", testPassword);
        var userEntity = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            Username = "testuser",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(testPassword),
            Role = Role.Shopper,
            IsActive = true
        };

        _mockUserRepository.Setup(repo => repo.GetByEmailAsync(request.Email, default)).ReturnsAsync(userEntity);

        var jti = Guid.NewGuid().ToString();
        var handler = new JwtSecurityTokenHandler();
        var token = new JwtSecurityToken(claims: new[] { new Claim(JwtRegisteredClaimNames.Jti, jti) });
        _mockTokenService.Setup(ts => ts.CreateToken(userEntity)).Returns(handler.WriteToken(token));

        // Act
        var (response, error) = await _authService.LoginAsync(request, "127.0.0.1", "Test Agent");

        // Assert
        Assert.Null(error);
        Assert.NotNull(response);
        _mockUserRepository.Verify(repo => repo.CreateSessionAsync(It.Is<Session>(s => s.TokenJtiHash == jti), default), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnError_WhenPasswordIsInvalid()
    {
        // Arrange
        var request = new LoginRequest("test@example.com", "WrongPassword");
        var userEntity = new User
        {
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            IsActive = true
        };
        _mockUserRepository.Setup(repo => repo.GetByEmailAsync(request.Email, default)).ReturnsAsync(userEntity);

        // Act
        var (response, error) = await _authService.LoginAsync(request, null, null);

        // Assert
        Assert.Null(response);
        Assert.NotNull(error);
        Assert.Equal("Email o contraseña incorrectos.", error);
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnError_WhenUserIsInactive()
    {
        // Arrange
        var testPassword = "Password123!";
        var request = new LoginRequest("inactive@example.com", testPassword);
        var userEntity = new User
        {
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(testPassword),
            IsActive = false // User is inactive
        };
        _mockUserRepository.Setup(repo => repo.GetByEmailAsync(request.Email, default)).ReturnsAsync(userEntity);

        // Act
        var (response, error) = await _authService.LoginAsync(request, null, null);

        // Assert
        Assert.Null(response);
        Assert.NotNull(error);
        Assert.Equal("La cuenta está inactiva. Por favor, contacte al soporte.", error);
    }

    [Fact]
    public async Task LogoutAsync_ShouldReturnSuccess_WhenSessionExists()
    {
        // Arrange
        var jti = Guid.NewGuid().ToString();
        var claims = new[] { new Claim(JwtRegisteredClaimNames.Jti, jti) };
        var identity = new ClaimsIdentity(claims);
        var userPrincipal = new ClaimsPrincipal(identity);
        var activeSession = new Session { Status = SessionStatus.Active };

        _mockUserRepository.Setup(repo => repo.GetActiveSessionByJtiAsync(jti, default)).ReturnsAsync(activeSession);

        // Act
        var (success, error) = await _authService.LogoutAsync(userPrincipal);

        // Assert
        Assert.True(success);
        Assert.Null(error);
        Assert.Equal(SessionStatus.Closed, activeSession.Status);
        _mockUserRepository.Verify(repo => repo.UpdateSessionAsync(activeSession, default), Times.Once);
    }

    [Fact]
    public async Task LogoutAsync_ShouldReturnError_WhenSessionNotFound()
    {
        // Arrange
        var jti = Guid.NewGuid().ToString();
        var claims = new[] { new Claim(JwtRegisteredClaimNames.Jti, jti) };
        var identity = new ClaimsIdentity(claims);
        var userPrincipal = new ClaimsPrincipal(identity);

        // Simulate that no active session is found for the token's JTI
        _mockUserRepository.Setup(repo => repo.GetActiveSessionByJtiAsync(jti, default)).ReturnsAsync((Session)null);

        // Act
        var (success, error) = await _authService.LogoutAsync(userPrincipal);

        // Assert
        Assert.False(success);
        Assert.NotNull(error);
        Assert.Contains("No se encontró una sesión activa", error);
    }

    // Tests for RegisterByAdminAsync are already correct and included
    [Fact]
    public async Task RegisterByAdminAsync_ShouldCreateEmployee_WhenRequestIsValid()
    {
        // Arrange
        var request = new AdminRegisterRequest("new.employee@example.com", "newemployee", "Password123!", "Employee");
        var userEntity = new User { Id = Guid.NewGuid(), Email = request.Email, Username = request.Username, Role = Role.Employee };

        _mockUserRepository.Setup(repo => repo.EmailExistsAsync(request.Email, default)).ReturnsAsync(false);
        _mockUserRepository.Setup(repo => repo.UsernameExistsAsync(request.Username, default)).ReturnsAsync(false);
        _mockUserRepository.Setup(repo => repo.AddAsync(It.IsAny<User>(), default)).ReturnsAsync(userEntity);
        _mockTokenService.Setup(ts => ts.CreateToken(It.IsAny<User>())).Returns("fake-admin-created-token");

        // Act
        var (response, error) = await _authService.RegisterByAdminAsync(request);

        // Assert
        Assert.Null(error);
        Assert.NotNull(response);
        _mockUserRepository.Verify(repo => repo.AddAsync(It.Is<User>(u => u.Role == Role.Employee), default), Times.Once);
    }

    [Fact]
    public async Task RegisterByAdminAsync_ShouldReturnError_WhenRoleIsNotEmployee()
    {
        // Arrange
        var request = new AdminRegisterRequest("hacker@example.com", "hacker", "Password123!", "SuperAdmin");

        // Act
        var (response, error) = await _authService.RegisterByAdminAsync(request);

        // Assert
        Assert.Null(response);
        Assert.NotNull(error);
        Assert.Contains("Rol inválido", error);
    }

    [Fact]
    public async Task GetActiveSessionAsync_ShouldReturnSession_WhenFound()
    {
        // Arrange
        var jti = Guid.NewGuid().ToString();
        var session = new Session { Id = Guid.NewGuid(), TokenJtiHash = jti };
        _mockUserRepository.Setup(repo => repo.GetActiveSessionByJtiAsync(jti, default)).ReturnsAsync(session);

        // Act
        var result = await _authService.GetActiveSessionAsync(jti);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(jti, result.TokenJtiHash);
    }
}