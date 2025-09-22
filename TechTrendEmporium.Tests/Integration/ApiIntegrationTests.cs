using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;
using System.Text.Json;
using TechTrendEmporium.Core.DTOs;
using TechTrendEmporium.Infrastructure.Data;

namespace TechTrendEmporium.Tests.Integration;

public class ApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                // Remove the existing DbContext registration
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Add in-memory database for testing
                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase("TestDatabase");
                });
            });
        });

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Register_ValidUser_ReturnsSuccess()
    {
        // Arrange
        var request = new RegisterRequest(
            "integration@test.com",
            "Password123!",
            "Password123!",
            "Integration",
            "Test");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<AuthenticationResult>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.Token);
        Assert.NotNull(result.UserId);
    }

    [Fact]
    public async Task GetCategories_ReturnsDefaultCategories()
    {
        // Act
        var response = await _client.GetAsync("/api/categories");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var categories = JsonSerializer.Deserialize<CategoryDto[]>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(categories);
        Assert.True(categories.Length >= 4); // We seeded 4 categories
        Assert.Contains(categories, c => c.Name == "Electronics");
        Assert.Contains(categories, c => c.Name == "Jewelery");
        Assert.Contains(categories, c => c.Name == "Men's Clothing");
        Assert.Contains(categories, c => c.Name == "Women's Clothing");
    }

    [Fact]
    public async Task GetProducts_ReturnsEmptyInitially()
    {
        // Act
        var response = await _client.GetAsync("/api/products");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var products = JsonSerializer.Deserialize<ProductDto[]>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(products);
        Assert.Empty(products); // No products initially
    }

    [Fact]
    public async Task GetCart_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/cart");

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AuthenticatedUser_CanAccessCart()
    {
        // Arrange - Register and login
        var registerRequest = new RegisterRequest(
            "cart@test.com",
            "Password123!",
            "Password123!",
            "Cart",
            "Test");

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        var registerContent = await registerResponse.Content.ReadAsStringAsync();
        var authResult = JsonSerializer.Deserialize<AuthenticationResult>(registerContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Add authorization header
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResult!.Token);

        // Act
        var response = await _client.GetAsync("/api/cart");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var cart = JsonSerializer.Deserialize<CartDto>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(cart);
        Assert.Equal(authResult.UserId, cart.UserId);
        Assert.Empty(cart.Items); // New cart should be empty
    }

    [Fact]
    public async Task Swagger_IsAccessible()
    {
        // Act
        var response = await _client.GetAsync("/swagger/v1/swagger.json");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        
        Assert.Contains("TechTrend Emporium API", content);
        Assert.Contains("E-commerce API", content);
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }
}