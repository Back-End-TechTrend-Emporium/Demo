using System.Net;
using System.Text.Json;
using Moq;
using Moq.Protected;
using TechTrendEmporium.Core.DTOs;
using TechTrendEmporium.Infrastructure.Services;

namespace TechTrendEmporium.Tests.Services;

public class FakeStoreApiServiceTests
{
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly FakeStoreApiService _service;

    public FakeStoreApiServiceTests()
    {
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        _service = new FakeStoreApiService(_httpClient);
    }

    [Fact]
    public async Task GetProductsAsync_WithValidResponse_ReturnsProducts()
    {
        // Arrange
        var products = new[]
        {
            new FakeStoreProductDto(
                1,
                "Test Product",
                19.95m,
                "A test product",
                "electronics",
                "https://example.com/image.jpg",
                new FakeStoreRatingDto(4.5m, 120))
        };

        var json = JsonSerializer.Serialize(products);
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json)
        };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        // Act
        var result = await _service.GetProductsAsync();

        // Assert
        Assert.Single(result);
        var product = result.First();
        Assert.Equal(1, product.Id);
        Assert.Equal("Test Product", product.Title);
        Assert.Equal(19.95m, product.Price);
        Assert.Equal("electronics", product.Category);
    }

    [Fact]
    public async Task GetProductsAsync_WithHttpError_ReturnsEmptyCollection()
    {
        // Arrange
        var httpResponse = new HttpResponseMessage(HttpStatusCode.InternalServerError);

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        // Act
        var result = await _service.GetProductsAsync();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetProductByIdAsync_WithValidResponse_ReturnsProduct()
    {
        // Arrange
        var productId = 1;
        var product = new FakeStoreProductDto(
            productId,
            "Test Product",
            19.95m,
            "A test product",
            "electronics",
            "https://example.com/image.jpg",
            new FakeStoreRatingDto(4.5m, 120));

        var json = JsonSerializer.Serialize(product);
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json)
        };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => 
                    req.RequestUri!.ToString().Contains($"products/{productId}")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        // Act
        var result = await _service.GetProductByIdAsync(productId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(productId, result.Id);
        Assert.Equal("Test Product", result.Title);
    }

    [Fact]
    public async Task GetProductByIdAsync_WithNotFound_ReturnsNull()
    {
        // Arrange
        var productId = 999;
        var httpResponse = new HttpResponseMessage(HttpStatusCode.NotFound);

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => 
                    req.RequestUri!.ToString().Contains($"products/{productId}")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        // Act
        var result = await _service.GetProductByIdAsync(productId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetCategoriesAsync_WithValidResponse_ReturnsCategories()
    {
        // Arrange
        var categories = new[] { "electronics", "jewelery", "men's clothing", "women's clothing" };
        var json = JsonSerializer.Serialize(categories);
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json)
        };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => 
                    req.RequestUri!.ToString().Contains("products/categories")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        // Act
        var result = await _service.GetCategoriesAsync();

        // Assert
        Assert.Equal(4, result.Count());
        Assert.Contains("electronics", result);
        Assert.Contains("jewelery", result);
        Assert.Contains("men's clothing", result);
        Assert.Contains("women's clothing", result);
    }

    [Fact]
    public async Task GetProductsByCategoryAsync_WithValidResponse_ReturnsProducts()
    {
        // Arrange
        var category = "electronics";
        var products = new[]
        {
            new FakeStoreProductDto(
                1,
                "Electronics Product",
                99.95m,
                "An electronics product",
                category,
                "https://example.com/image.jpg",
                new FakeStoreRatingDto(4.0m, 50))
        };

        var json = JsonSerializer.Serialize(products);
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json)
        };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => 
                    req.RequestUri!.ToString().Contains($"products/category/{category}")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        // Act
        var result = await _service.GetProductsByCategoryAsync(category);

        // Assert
        Assert.Single(result);
        var product = result.First();
        Assert.Equal(category, product.Category);
        Assert.Equal("Electronics Product", product.Title);
    }
}