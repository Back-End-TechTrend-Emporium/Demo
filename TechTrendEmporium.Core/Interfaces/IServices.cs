using TechTrendEmporium.Core.DTOs;
using TechTrendEmporium.Core.Entities;

namespace TechTrendEmporium.Core.Interfaces;

public interface IFakeStoreApiService
{
    Task<IEnumerable<FakeStoreProductDto>> GetProductsAsync();
    Task<FakeStoreProductDto?> GetProductByIdAsync(int id);
    Task<IEnumerable<string>> GetCategoriesAsync();
    Task<IEnumerable<FakeStoreProductDto>> GetProductsByCategoryAsync(string category);
}

public interface IAuthenticationService
{
    Task<AuthenticationResult> LoginAsync(LoginRequest request);
    Task<AuthenticationResult> RegisterAsync(RegisterRequest request);
    Task LogoutAsync(string userId, string sessionId);
    Task<string> GenerateJwtTokenAsync(string userId, IEnumerable<string> roles);
    Task<bool> ValidateTokenAsync(string token);
}

public interface ICartService
{
    Task<CartDto> GetCartAsync(string userId);
    Task<CartDto> AddItemAsync(string userId, AddCartItemRequest request);
    Task<CartDto> UpdateItemAsync(string userId, int productId, int quantity);
    Task<CartDto> RemoveItemAsync(string userId, int productId);
    Task ClearCartAsync(string userId);
    Task<decimal> CalculateCartTotalAsync(string userId, string? couponCode = null);
}

public interface IOrderService
{
    Task<OrderDto> CreateOrderAsync(string userId, CreateOrderRequest request);
    Task<OrderDto?> GetOrderByIdAsync(int orderId);
    Task<IEnumerable<OrderDto>> GetUserOrdersAsync(string userId);
    Task<OrderDto> UpdateOrderStatusAsync(int orderId, OrderStatus status);
    Task<IEnumerable<OrderDto>> GetOrdersByStatusAsync(OrderStatus status);
}

public interface IProductService
{
    Task<IEnumerable<ProductDto>> GetProductsAsync(ProductFilterRequest? filter = null);
    Task<ProductDto?> GetProductByIdAsync(int id);
    Task<ProductDto> CreateProductAsync(CreateProductRequest request);
    Task<ProductDto> UpdateProductAsync(int id, UpdateProductRequest request);
    Task DeleteProductAsync(int id);
    Task<IEnumerable<ProductDto>> SearchProductsAsync(string searchTerm);
    Task SyncWithFakeStoreApiAsync();
}

public interface ICategoryService
{
    Task<IEnumerable<CategoryDto>> GetCategoriesAsync();
    Task<CategoryDto?> GetCategoryByIdAsync(int id);
    Task<CategoryDto> CreateCategoryAsync(CreateCategoryRequest request);
    Task<CategoryDto> UpdateCategoryAsync(int id, UpdateCategoryRequest request);
    Task DeleteCategoryAsync(int id);
    Task SyncWithFakeStoreApiAsync();
}

public interface IReviewService
{
    Task<IEnumerable<ReviewDto>> GetProductReviewsAsync(int productId);
    Task<ReviewDto> CreateReviewAsync(string userId, CreateReviewRequest request);
    Task<ReviewDto> UpdateReviewAsync(int reviewId, UpdateReviewRequest request);
    Task DeleteReviewAsync(int reviewId);
    Task<ReviewDto> ApproveReviewAsync(int reviewId);
    Task<IEnumerable<ReviewDto>> GetPendingReviewsAsync();
}

public interface IWishlistService
{
    Task<IEnumerable<WishlistItemDto>> GetWishlistAsync(string userId);
    Task<WishlistItemDto> AddToWishlistAsync(string userId, int productId);
    Task RemoveFromWishlistAsync(string userId, int productId);
    Task<bool> IsInWishlistAsync(string userId, int productId);
}

public interface ICouponService
{
    Task<CouponDto?> GetCouponByCodeAsync(string code);
    Task<bool> ValidateCouponAsync(string code, decimal orderAmount);
    Task<decimal> CalculateDiscountAsync(string code, decimal orderAmount);
    Task<CouponDto> CreateCouponAsync(CreateCouponRequest request);
    Task<CouponDto> UpdateCouponAsync(int id, UpdateCouponRequest request);
    Task DeleteCouponAsync(int id);
}

public interface IUserManagementService
{
    Task<IEnumerable<UserDto>> GetUsersAsync();
    Task<UserDto?> GetUserByIdAsync(string id);
    Task<UserDto> CreateUserAsync(CreateUserRequest request);
    Task<UserDto> UpdateUserAsync(string id, UpdateUserRequest request);
    Task DeleteUserAsync(string id);
    Task<UserDto> AssignRoleAsync(string userId, string role);
    Task<UserDto> RemoveRoleAsync(string userId, string role);
    Task<IEnumerable<string>> GetUserRolesAsync(string userId);
}