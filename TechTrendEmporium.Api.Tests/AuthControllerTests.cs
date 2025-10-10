using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Logica.Interfaces;
using Logica.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using TechTrendEmporium.Api.Controllers;
using Xunit;

namespace TechTrendEmporium.Api.Tests.Controllers;

public class AuthControllerTests
{
    // Mocks for the controller dependencies
    private readonly Mock<IAuthService> _mockAuthService;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ILogger<AuthController>> _mockLogger;

    // The controller instance to be tested
    private readonly AuthController _authController;

    public AuthControllerTests()
    {
        // Initialize mocks for a clean test environment
        _mockAuthService = new Mock<IAuthService>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<AuthController>>();

        // Create the controller instance with mocked dependencies
        _authController = new AuthController(
            _mockAuthService.Object,
            _mockConfiguration.Object,
            _mockLogger.Object);

        // Mock HttpContext for methods that need it (like Login)
        _authController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    private ClaimsPrincipal CreateClaimsPrincipal(string userId, string role)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Role, role)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        return new ClaimsPrincipal(identity);
    }

    [Fact]
    public async Task RegisterShopper_ShouldReturnOk_WhenRegistrationSucceeds()
    {
        // Arrange
        var request = new ShopperRegisterRequest("test@example.com", "testuser", "Password123!");
        var authResponse = new AuthResponse(Guid.NewGuid(), request.Email, request.Username, "Shopper", "fake-token");

        // Setup the service mock to return a successful response
        _mockAuthService.Setup(s => s.RegisterShopperAsync(request)).ReturnsAsync((authResponse, null));

        // Act
        var result = await _authController.RegisterShopper(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task RegisterShopper_ShouldReturnBadRequest_WhenRegistrationFails()
    {
        // Arrange
        var request = new ShopperRegisterRequest("test@example.com", "testuser", "Password123!");

        // Setup the service mock to return an error
        _mockAuthService.Setup(s => s.RegisterShopperAsync(request)).ReturnsAsync((null, "User already exists."));

        // Act
        var result = await _authController.RegisterShopper(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task Login_ShouldReturnOkWithToken_WhenCredentialsAreValid()
    {
        // Arrange
        var request = new LoginRequest("test@example.com", "Password123!");
        var authResponse = new AuthResponse(Guid.NewGuid(), request.Email, "testuser", "Shopper", "fake-token");

        // Setup the service mock for a successful login
        _mockAuthService.Setup(s => s.LoginAsync(request, It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync((authResponse, null));

        // Act
        var result = await _authController.Login(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task Login_ShouldReturnUnauthorized_WhenCredentialsAreInvalid()
    {
        // Arrange
        var request = new LoginRequest("test@example.com", "WrongPassword");

        // Setup the service mock for a failed login
        _mockAuthService.Setup(s => s.LoginAsync(request, It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync((null, "Invalid credentials."));

        // Act
        var result = await _authController.Login(request);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task Logout_ShouldReturnOk_WhenLogoutSucceeds()
    {
        // Arrange
        // Simulate an authenticated user
        var userId = Guid.NewGuid();
        _authController.ControllerContext.HttpContext.User = CreateClaimsPrincipal(userId.ToString(), "Shopper");

        // Setup the service for a successful logout
        _mockAuthService.Setup(s => s.LogoutAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync((true, null));

        // Act
        var result = await _authController.Logout();

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Logout_ShouldReturnBadRequest_WhenLogoutFails()
    {
        // Arrange
        // Simulate an authenticated user
        var userId = Guid.NewGuid();
        _authController.ControllerContext.HttpContext.User = CreateClaimsPrincipal(userId.ToString(), "Shopper");

        // Setup the service for a failed logout
        _mockAuthService.Setup(s => s.LogoutAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync((false, "Session not found."));

        // Act
        var result = await _authController.Logout();

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }
}