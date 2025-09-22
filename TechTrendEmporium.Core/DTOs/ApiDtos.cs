using TechTrendEmporium.Core.Entities;

namespace TechTrendEmporium.Core.DTOs;

// Authentication DTOs
public record LoginRequest(string Email, string Password, bool RememberMe = false);

public record RegisterRequest(
    string Email,
    string Password,
    string ConfirmPassword,
    string FirstName,
    string LastName);

public record AuthenticationResult(
    bool Success,
    string? Token,
    string? UserId,
    IEnumerable<string>? Roles,
    string? ErrorMessage,
    DateTime? ExpiresAt);

// User DTOs
public record UserDto(
    string Id,
    string Email,
    string FirstName,
    string LastName,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? LastLoginAt,
    IEnumerable<string> Roles);

public record CreateUserRequest(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string Role = "Shopper");

public record UpdateUserRequest(
    string FirstName,
    string LastName,
    bool IsActive);

// Product DTOs
public record ProductDto(
    int Id,
    string Title,
    string Description,
    decimal Price,
    string? ImageUrl,
    int CategoryId,
    string CategoryName,
    bool IsActive,
    int StockQuantity,
    double AverageRating,
    int ReviewCount,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record CreateProductRequest(
    string Title,
    string Description,
    decimal Price,
    string? ImageUrl,
    int CategoryId,
    int StockQuantity);

public record UpdateProductRequest(
    string Title,
    string Description,
    decimal Price,
    string? ImageUrl,
    int CategoryId,
    int StockQuantity,
    bool IsActive);

public record ProductFilterRequest(
    int? CategoryId = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    string? SearchTerm = null,
    int Page = 1,
    int PageSize = 10,
    string SortBy = "Title",
    bool SortDescending = false);

// Category DTOs
public record CategoryDto(
    int Id,
    string Name,
    string Description,
    string? ImageUrl,
    bool IsActive,
    int ProductCount,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record CreateCategoryRequest(
    string Name,
    string Description,
    string? ImageUrl);

public record UpdateCategoryRequest(
    string Name,
    string Description,
    string? ImageUrl,
    bool IsActive);

// Cart DTOs
public record CartDto(
    int Id,
    string UserId,
    IEnumerable<CartItemDto> Items,
    decimal SubTotal,
    decimal DiscountAmount,
    decimal Total,
    DateTime UpdatedAt);

public record CartItemDto(
    int Id,
    int ProductId,
    string ProductTitle,
    string? ProductImageUrl,
    decimal UnitPrice,
    int Quantity,
    decimal TotalPrice,
    DateTime AddedAt);

public record AddCartItemRequest(
    int ProductId,
    int Quantity);

// Order DTOs
public record OrderDto(
    int Id,
    string UserId,
    DateTime OrderDate,
    OrderStatus Status,
    decimal SubTotal,
    decimal DiscountAmount,
    decimal TotalAmount,
    string? CouponCode,
    string ShippingAddress,
    IEnumerable<OrderItemDto> Items);

public record OrderItemDto(
    int Id,
    int ProductId,
    string ProductTitle,
    decimal UnitPrice,
    int Quantity,
    decimal TotalPrice);

public record CreateOrderRequest(
    string ShippingAddress,
    string? CouponCode);

// Review DTOs
public record ReviewDto(
    int Id,
    string UserId,
    string UserName,
    int ProductId,
    int Rating,
    string Comment,
    DateTime CreatedAt,
    bool IsApproved);

public record CreateReviewRequest(
    int ProductId,
    int Rating,
    string Comment);

public record UpdateReviewRequest(
    int Rating,
    string Comment);

// Wishlist DTOs
public record WishlistItemDto(
    int Id,
    string UserId,
    int ProductId,
    string ProductTitle,
    string? ProductImageUrl,
    decimal ProductPrice,
    DateTime AddedAt);

// Coupon DTOs
public record CouponDto(
    int Id,
    string Code,
    string Description,
    decimal DiscountPercentage,
    decimal? MaxDiscountAmount,
    decimal? MinimumOrderAmount,
    DateTime ValidFrom,
    DateTime ValidTo,
    int UsageLimit,
    int UsedCount,
    bool IsActive);

public record CreateCouponRequest(
    string Code,
    string Description,
    decimal DiscountPercentage,
    decimal? MaxDiscountAmount,
    decimal? MinimumOrderAmount,
    DateTime ValidFrom,
    DateTime ValidTo,
    int UsageLimit);

public record UpdateCouponRequest(
    string Description,
    decimal DiscountPercentage,
    decimal? MaxDiscountAmount,
    decimal? MinimumOrderAmount,
    DateTime ValidFrom,
    DateTime ValidTo,
    int UsageLimit,
    bool IsActive);

// FakeStore API DTOs
public record FakeStoreProductDto(
    int Id,
    string Title,
    decimal Price,
    string Description,
    string Category,
    string Image,
    FakeStoreRatingDto Rating);

public record FakeStoreRatingDto(
    decimal Rate,
    int Count);