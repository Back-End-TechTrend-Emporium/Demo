using Microsoft.EntityFrameworkCore;
using TechTrendEmporium.Core.Entities;
using TechTrendEmporium.Infrastructure.Data;
using TechTrendEmporium.Infrastructure.Repositories;

namespace TechTrendEmporium.Tests.Repositories;

public class ProductRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly ProductRepository _repository;

    public ProductRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _repository = new ProductRepository(_context);

        SeedData();
    }

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsProduct()
    {
        // Arrange
        var productId = 1;

        // Act
        var result = await _repository.GetByIdAsync(productId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(productId, result.Id);
        Assert.Equal("Test Product 1", result.Title);
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange
        var productId = 999;

        // Act
        var result = await _repository.GetByIdAsync(productId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsActiveProductsOnly()
    {
        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        Assert.Equal(2, result.Count()); // Only active products
        Assert.All(result, p => Assert.True(p.IsActive));
    }

    [Fact]
    public async Task GetByCategoryAsync_WithValidCategoryId_ReturnsProductsInCategory()
    {
        // Arrange
        var categoryId = 1;

        // Act
        var result = await _repository.GetByCategoryAsync(categoryId);

        // Assert
        Assert.Single(result);
        Assert.Equal(categoryId, result.First().CategoryId);
    }

    [Fact]
    public async Task SearchAsync_WithMatchingTerm_ReturnsMatchingProducts()
    {
        // Arrange
        var searchTerm = "Product 1";

        // Act
        var result = await _repository.SearchAsync(searchTerm);

        // Assert
        Assert.Single(result);
        Assert.Contains(searchTerm, result.First().Title);
    }

    [Fact]
    public async Task GetByExternalIdAsync_WithValidExternalId_ReturnsProduct()
    {
        // Arrange
        var externalId = 101;

        // Act
        var result = await _repository.GetByExternalIdAsync(externalId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(externalId, result.ExternalId);
    }

    [Fact]
    public async Task AddAsync_WithValidProduct_AddsToDatabase()
    {
        // Arrange
        var product = new Product
        {
            Title = "New Product",
            Description = "New Description",
            Price = 99.99m,
            CategoryId = 1,
            IsActive = true,
            StockQuantity = 10,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        await _repository.AddAsync(product);
        await _context.SaveChangesAsync();

        // Assert
        var addedProduct = await _context.Products.FindAsync(product.Id);
        Assert.NotNull(addedProduct);
        Assert.Equal("New Product", addedProduct.Title);
    }

    private void SeedData()
    {
        var categories = new List<Category>
        {
            new() { Id = 1, Name = "Electronics", Description = "Electronic devices", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new() { Id = 2, Name = "Clothing", Description = "Clothing items", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        };

        var products = new List<Product>
        {
            new()
            {
                Id = 1,
                Title = "Test Product 1",
                Description = "Description 1",
                Price = 100.00m,
                CategoryId = 1,
                IsActive = true,
                StockQuantity = 10,
                ExternalId = 101,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = 2,
                Title = "Test Product 2",
                Description = "Description 2",
                Price = 200.00m,
                CategoryId = 2,
                IsActive = true,
                StockQuantity = 20,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = 3,
                Title = "Inactive Product",
                Description = "Inactive Description",
                Price = 300.00m,
                CategoryId = 1,
                IsActive = false,
                StockQuantity = 5,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        _context.Categories.AddRange(categories);
        _context.Products.AddRange(products);
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}