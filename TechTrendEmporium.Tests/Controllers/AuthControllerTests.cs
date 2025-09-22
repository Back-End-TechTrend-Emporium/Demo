using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Moq;
using TechTrendEmporium.API.Controllers;
using TechTrendEmporium.Core.DTOs;
using TechTrendEmporium.Core.Entities;

namespace TechTrendEmporium.Tests.Controllers;

public class AuthControllerTests
{
    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly Mock<SignInManager<User>> _signInManagerMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _userManagerMock = MockUserManager();
        _signInManagerMock = MockSignInManager();
        _configurationMock = new Mock<IConfiguration>();
        
        // Setup configuration
        _configurationMock.Setup(c => c["Jwt:Key"]).Returns("test-key-that-is-long-enough-for-hmac-sha256-algorithm");
        _configurationMock.Setup(c => c["Jwt:Issuer"]).Returns("TestIssuer");
        _configurationMock.Setup(c => c["Jwt:Audience"]).Returns("TestAudience");
        
        _controller = new AuthController(_userManagerMock.Object, _signInManagerMock.Object, _configurationMock.Object);
    }

    [Fact]
    public async Task Register_WithValidRequest_ReturnsOkWithToken()
    {
        // Arrange
        var request = new RegisterRequest(
            "test@example.com",
            "Password123!",
            "Password123!",
            "Test",
            "User");

        _userManagerMock.Setup(um => um.FindByEmailAsync(request.Email))
            .ReturnsAsync((User?)null);

        _userManagerMock.Setup(um => um.CreateAsync(It.IsAny<User>(), request.Password))
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock.Setup(um => um.AddToRoleAsync(It.IsAny<User>(), "Shopper"))
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock.Setup(um => um.GetRolesAsync(It.IsAny<User>()))
            .ReturnsAsync(new List<string> { "Shopper" });

        // Act
        var result = await _controller.Register(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var authResult = Assert.IsType<AuthenticationResult>(okResult.Value);
        Assert.True(authResult.Success);
        Assert.NotNull(authResult.Token);
        Assert.Contains("Shopper", authResult.Roles!);
    }

    [Fact]
    public async Task Register_WithMismatchedPasswords_ReturnsBadRequest()
    {
        // Arrange
        var request = new RegisterRequest(
            "test@example.com",
            "Password123!",
            "DifferentPassword123!",
            "Test",
            "User");

        // Act
        var result = await _controller.Register(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var authResult = Assert.IsType<AuthenticationResult>(badRequestResult.Value);
        Assert.False(authResult.Success);
        Assert.Equal("Passwords do not match", authResult.ErrorMessage);
    }

    [Fact]
    public async Task Register_WithExistingEmail_ReturnsBadRequest()
    {
        // Arrange
        var request = new RegisterRequest(
            "existing@example.com",
            "Password123!",
            "Password123!",
            "Test",
            "User");

        var existingUser = new User { Email = request.Email };
        _userManagerMock.Setup(um => um.FindByEmailAsync(request.Email))
            .ReturnsAsync(existingUser);

        // Act
        var result = await _controller.Register(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var authResult = Assert.IsType<AuthenticationResult>(badRequestResult.Value);
        Assert.False(authResult.Success);
        Assert.Equal("User with this email already exists", authResult.ErrorMessage);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsOkWithToken()
    {
        // Arrange
        var request = new LoginRequest("test@example.com", "Password123!");
        var user = new User 
        { 
            Id = "123",
            Email = request.Email,
            FirstName = "Test",
            LastName = "User",
            IsActive = true
        };

        _userManagerMock.Setup(um => um.FindByEmailAsync(request.Email))
            .ReturnsAsync(user);

        _userManagerMock.Setup(um => um.CheckPasswordAsync(user, request.Password))
            .ReturnsAsync(true);

        _userManagerMock.Setup(um => um.UpdateAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock.Setup(um => um.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "Shopper" });

        // Act
        var result = await _controller.Login(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var authResult = Assert.IsType<AuthenticationResult>(okResult.Value);
        Assert.True(authResult.Success);
        Assert.NotNull(authResult.Token);
        Assert.Equal(user.Id, authResult.UserId);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var request = new LoginRequest("test@example.com", "WrongPassword");

        _userManagerMock.Setup(um => um.FindByEmailAsync(request.Email))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _controller.Login(request);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        var authResult = Assert.IsType<AuthenticationResult>(unauthorizedResult.Value);
        Assert.False(authResult.Success);
        Assert.Equal("Invalid email or password", authResult.ErrorMessage);
    }

    [Fact]
    public async Task Login_WithInactiveUser_ReturnsUnauthorized()
    {
        // Arrange
        var request = new LoginRequest("test@example.com", "Password123!");
        var user = new User 
        { 
            Email = request.Email,
            IsActive = false
        };

        _userManagerMock.Setup(um => um.FindByEmailAsync(request.Email))
            .ReturnsAsync(user);

        _userManagerMock.Setup(um => um.CheckPasswordAsync(user, request.Password))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Login(request);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        var authResult = Assert.IsType<AuthenticationResult>(unauthorizedResult.Value);
        Assert.False(authResult.Success);
        Assert.Equal("Account is deactivated", authResult.ErrorMessage);
    }

    private static Mock<UserManager<User>> MockUserManager()
    {
        var store = new Mock<IUserStore<User>>();
        return new Mock<UserManager<User>>(store.Object, null, null, null, null, null, null, null, null);
    }

    private static Mock<SignInManager<User>> MockSignInManager()
    {
        var userManager = MockUserManager();
        var contextAccessor = new Mock<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
        var claimsFactory = new Mock<IUserClaimsPrincipalFactory<User>>();
        
        return new Mock<SignInManager<User>>(
            userManager.Object,
            contextAccessor.Object,
            claimsFactory.Object,
            null, null, null, null);
    }
}