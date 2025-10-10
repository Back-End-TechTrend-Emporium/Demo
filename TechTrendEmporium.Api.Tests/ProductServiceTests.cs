using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Data;
using Data.Entities;
using Data.Entities.Enums;
using External.FakeStore;
using Logica.Interfaces;
using Logica.Models.Products;
using Logica.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.EntityFrameworkCore; // Required for DbContext mocking
using Xunit;

namespace TechTrendEmporium.Api.Tests.Services;

public class ProductServiceTests
{
    // Mocks for all dependencies
    private readonly Mock<IFakeStoreApiService> _mockFakeStoreClient;
    private readonly Mock<IProductRepository> _mockProductRepository;
    private readonly Mock<AppDbContext> _mockContext;
    private readonly Mock<ILogger<ProductService>> _mockLogger;

    // The service instance we are testing
    private readonly ProductService _productService;

    public ProductServiceTests()
    {
        // Initialize mocks
        _mockFakeStoreClient = new Mock<IFakeStoreApiService>();
        _mockProductRepository = new Mock<IProductRepository>();
        _mockContext = new Mock<AppDbContext>(new DbContextOptions<AppDbContext>());
        _mockLogger = new Mock<ILogger<ProductService>>();

        // Initialize the service with the mocked dependencies
        _productService = new ProductService(
            _mockFakeStoreClient.Object,
            _mockProductRepository.Object,
            _mockContext.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task GetProductByIdAsync_ShouldReturnProductDto_WhenProductExists()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var productEntity = new Product { Id = productId, Title = "Test Product" };

        // Setup the repository mock to return our test product.
        _mockProductRepository.Setup(repo => repo.GetByIdAsync(productId)).ReturnsAsync(productEntity);

        // Act
        var result = await _productService.GetProductByIdAsync(productId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(productId, result.Id);
        Assert.Equal("Test Product", result.Title);
    }

    [Fact]
    public async Task GetProductByIdAsync_ShouldReturnNull_WhenProductDoesNotExist()
    {
        // Arrange
        // Setup the repository mock to return null.
        _mockProductRepository.Setup(repo => repo.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Product)null);

        // Act
        var result = await _productService.GetProductByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateProductAsync_ShouldCreateProductWithPendingState_AndCreateNewCategory()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productDto = new ProductCreateDto
        {
            Title = "New Gadget",
            Category = "New Tech"
            // ... other properties
        };

        var createdProductEntity = new Product { Id = Guid.NewGuid(), Title = productDto.Title, State = ApprovalState.PendingApproval };

        // Setup mock for GetOrCreateCategoryAsync: simulate category does not exist.
        var categories = new List<Category>();
        _mockContext.Setup(x => x.Categories).ReturnsDbSet(categories);

        // Setup mock for the repository's CreateAsync method.
        _mockProductRepository.Setup(repo => repo.CreateAsync(It.IsAny<Product>())).ReturnsAsync(createdProductEntity);

        // Act
        var result = await _productService.CreateProductAsync(productDto, userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("New Gadget", result.Title);
        // Verify that the product is created with PendingApproval state.
        _mockProductRepository.Verify(repo => repo.CreateAsync(It.Is<Product>(p => p.State == ApprovalState.PendingApproval)), Times.Once);
        // Verify that a new category was added to the context.
        _mockContext.Verify(x => x.Categories.Add(It.Is<Category>(c => c.Name == "New Tech")), Times.Once);
        // Verify that SaveChanges was called.
        _mockContext.Verify(x => x.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task ApproveProductAsync_ShouldChangeStateToApproved_WhenProductExists()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var approverId = Guid.NewGuid();
        var productToApprove = new Product { Id = productId, State = ApprovalState.PendingApproval };

        // Setup the repository to find the product.
        _mockProductRepository.Setup(repo => repo.GetByIdAsync(productId)).ReturnsAsync(productToApprove);

        // Act
        var result = await _productService.ApproveProductAsync(productId, approverId);

        // Assert
        Assert.True(result);
        // Verify that the product's state was changed to Approved.
        Assert.Equal(ApprovalState.Approved, productToApprove.State);
        Assert.Equal(approverId, productToApprove.ApprovedBy);
        // Verify that the UpdateAsync method was called on the repository.
        _mockProductRepository.Verify(repo => repo.UpdateAsync(It.Is<Product>(p => p.State == ApprovalState.Approved)), Times.Once);
    }
}