using External.FakeStore.Models;

namespace Logica.Interfaces
{
    public interface ICartService
    {
        // === FakeStore API Operations ===
        Task<IEnumerable<FakeStoreCartResponse>> GetCartsFromFakeStoreAsync();
        Task<FakeStoreCartResponse?> GetCartFromFakeStoreAsync(int cartId);
        Task<FakeStoreCartResponse?> CreateCartInFakeStoreAsync(FakeStoreCartCreateRequest cartRequest);
        Task<FakeStoreCartResponse?> UpdateCartInFakeStoreAsync(int cartId, FakeStoreCartUpdateRequest cartRequest);
        Task<FakeStoreCartResponse?> DeleteCartInFakeStoreAsync(int cartId);

        // === Product Validation for FakeStore Operations ===
        Task<bool> ValidateProductsExistInLocalDbAsync(IEnumerable<int> fakeStoreProductIds);
        Task<Dictionary<int, Guid>> MapFakeStoreProductIdsToLocalAsync(IEnumerable<int> fakeStoreProductIds);

        // === Local Cart Operations (TODO: Implement these in CartRepository) ===
        // Task<CartDto?> GetActiveCartByUserIdAsync(Guid userId);
        // Task<CartDto> CreateCartAsync(Guid userId);
        // Task<CartDto?> AddItemToCartAsync(Guid userId, Guid productId, int quantity);
        // Task<CartDto?> UpdateCartItemQuantityAsync(Guid userId, Guid productId, int quantity);
        // Task<bool> RemoveItemFromCartAsync(Guid userId, Guid productId);
        // Task<bool> ClearCartAsync(Guid userId);
        // Task<CartDto?> ApplyCouponAsync(Guid userId, string couponCode);
        // Task<CartDto?> RemoveCouponAsync(Guid userId);
        // Task<IEnumerable<CartDto>> GetCartHistoryByUserIdAsync(Guid userId);
        // Task<bool> CheckoutCartAsync(Guid userId);
    }
}