using Data.Entities;

namespace Logica.Interfaces
{
    public interface ICartRepository
    {
        // === TODO: Implement these main cart repository operations ===
        
        // === Cart CRUD Operations ===
        // Task<Cart?> GetActiveCartByUserIdAsync(Guid userId);
        // Task<Cart?> GetCartByIdAsync(Guid cartId);
        // Task<Cart> CreateCartAsync(Cart cart);
        // Task<Cart> UpdateCartAsync(Cart cart);
        // Task<bool> DeleteCartAsync(Guid cartId);
        // Task<IEnumerable<Cart>> GetCartsByUserIdAsync(Guid userId);
        
        // === CartItem Operations ===
        // Task<CartItem?> GetCartItemAsync(Guid cartId, Guid productId);
        // Task<CartItem> AddCartItemAsync(CartItem cartItem);
        // Task<CartItem> UpdateCartItemAsync(CartItem cartItem);
        // Task<bool> RemoveCartItemAsync(Guid cartItemId);
        // Task<bool> RemoveAllCartItemsAsync(Guid cartId);
        
        // === Cart Status Management ===
        // Task<bool> UpdateCartStatusAsync(Guid cartId, CartStatus status);
        // Task<Cart?> GetCartWithItemsAsync(Guid cartId);
        
        // === Coupon Operations ===
        // Task<bool> ApplyCouponToCartAsync(Guid cartId, Guid couponId);
        // Task<bool> RemoveCouponFromCartAsync(Guid cartId);
        
        // === Cart Calculations ===
        // Task<decimal> CalculateCartTotalAsync(Guid cartId);
        // Task<bool> UpdateCartTotalsAsync(Guid cartId);
    }
}