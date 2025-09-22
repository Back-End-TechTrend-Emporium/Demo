using Microsoft.EntityFrameworkCore;
using TechTrendEmporium.Core.Entities;
using TechTrendEmporium.Infrastructure.Data;
using TechTrendEmporium.Infrastructure.Repositories;

namespace TechTrendEmporium.Tests.Repositories;

public class CartRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly CartRepository _repository;

    public CartRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _repository = new CartRepository(_context);

        SeedData();
    }

    [Fact]
    public async Task GetActiveCartByUserIdAsync_WithValidUserId_ReturnsActiveCart()
    {
        // Arrange
        var userId = "user1";

        // Act
        var result = await _repository.GetActiveCartByUserIdAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.UserId);
        Assert.True(result.IsActive);
        Assert.NotEmpty(result.Items);
    }

    [Fact]
    public async Task GetActiveCartByUserIdAsync_WithInvalidUserId_ReturnsNull()
    {
        // Arrange
        var userId = "nonexistent";

        // Act
        var result = await _repository.GetActiveCartByUserIdAsync(userId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetCartItemsAsync_WithValidCartId_ReturnsCartItems()
    {
        // Arrange
        var cartId = 1;

        // Act
        var result = await _repository.GetCartItemsAsync(cartId);

        // Assert
        Assert.Equal(2, result.Count());
        Assert.All(result, item => Assert.Equal(cartId, item.CartId));
    }

    [Fact]
    public async Task AddItemAsync_WithValidCartItem_AddsToDatabase()
    {
        // Arrange
        var cartItem = new CartItem
        {
            CartId = 1,
            ProductId = 3,
            Quantity = 1,
            UnitPrice = 50.00m,
            AddedAt = DateTime.UtcNow
        };

        // Act
        await _repository.AddItemAsync(cartItem);
        await _context.SaveChangesAsync();

        // Assert
        var addedItem = await _context.CartItems.FindAsync(cartItem.Id);
        Assert.NotNull(addedItem);
        Assert.Equal(3, addedItem.ProductId);
    }

    [Fact]
    public async Task UpdateItemAsync_WithValidCartItem_UpdatesInDatabase()
    {
        // Arrange
        var cartItem = await _context.CartItems.FirstAsync();
        var newQuantity = 10;
        cartItem.Quantity = newQuantity;

        // Act
        await _repository.UpdateItemAsync(cartItem);
        await _context.SaveChangesAsync();

        // Assert
        var updatedItem = await _context.CartItems.FindAsync(cartItem.Id);
        Assert.NotNull(updatedItem);
        Assert.Equal(newQuantity, updatedItem.Quantity);
    }

    [Fact]
    public async Task RemoveItemAsync_WithValidItemId_RemovesFromDatabase()
    {
        // Arrange
        var itemId = 1;
        var itemCountBefore = await _context.CartItems.CountAsync();

        // Act
        await _repository.RemoveItemAsync(itemId);
        await _context.SaveChangesAsync();

        // Assert
        var itemCountAfter = await _context.CartItems.CountAsync();
        Assert.Equal(itemCountBefore - 1, itemCountAfter);
        
        var removedItem = await _context.CartItems.FindAsync(itemId);
        Assert.Null(removedItem);
    }

    [Fact]
    public async Task ClearCartAsync_WithValidCartId_RemovesAllItems()
    {
        // Arrange
        var cartId = 1;

        // Act
        await _repository.ClearCartAsync(cartId);
        await _context.SaveChangesAsync();

        // Assert
        var remainingItems = await _context.CartItems.Where(i => i.CartId == cartId).ToListAsync();
        Assert.Empty(remainingItems);
    }

    private void SeedData()
    {
        var categories = new List<Category>
        {
            new() { Id = 1, Name = "Electronics", Description = "Electronic devices", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        };

        var products = new List<Product>
        {
            new()
            {
                Id = 1,
                Title = "Product 1",
                Description = "Description 1",
                Price = 100.00m,
                CategoryId = 1,
                IsActive = true,
                StockQuantity = 10,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = 2,
                Title = "Product 2",
                Description = "Description 2",
                Price = 200.00m,
                CategoryId = 1,
                IsActive = true,
                StockQuantity = 20,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        var carts = new List<Cart>
        {
            new()
            {
                Id = 1,
                UserId = "user1",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = 2,
                UserId = "user2",
                IsActive = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        var cartItems = new List<CartItem>
        {
            new()
            {
                Id = 1,
                CartId = 1,
                ProductId = 1,
                Quantity = 2,
                UnitPrice = 100.00m,
                AddedAt = DateTime.UtcNow
            },
            new()
            {
                Id = 2,
                CartId = 1,
                ProductId = 2,
                Quantity = 1,
                UnitPrice = 200.00m,
                AddedAt = DateTime.UtcNow
            }
        };

        _context.Categories.AddRange(categories);
        _context.Products.AddRange(products);
        _context.Carts.AddRange(carts);
        _context.CartItems.AddRange(cartItems);
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}