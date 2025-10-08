using Data.Entities;
using Data.Entities.Enums;

namespace Logica.Interfaces
{
    public interface ICartRepository
    {
        // Basic Cart Operations
        Task<Cart?> GetCartByIdAsync(Guid cartId);
        Task<IEnumerable<Cart>> GetAllCartsAsync();
        Task<IEnumerable<Cart>> GetCartsByUserIdAsync(Guid userId);
        Task<Cart?> GetActiveCartByUserIdAsync(Guid userId);
        Task<Cart> CreateCartAsync(Cart cart);
        Task<Cart> UpdateCartAsync(Cart cart);
        Task<bool> DeleteCartAsync(Guid cartId);
        Task<bool> SoftDeleteCartAsync(Guid cartId);

        // External Mapping Operations
        Task<Cart?> GetCartByExternalIdAsync(string externalId, ExternalSource source);
        Task<bool> ExternalCartExistsAsync(string externalId, ExternalSource source);
        Task<ExternalMapping> CreateCartMappingAsync(string externalId, Guid localCartId, ExternalSource source, string snapshotJson);

        // Direct cart item operations to avoid concurrency issues
        Task<CartItem> AddOrUpdateCartItemAsync(Guid cartId, Guid productId, int quantity, decimal unitPrice, string? title = null, string? imageUrl = null, string? categoryName = null);
        Task<bool> RemoveCartItemAsync(Guid cartId, Guid productId);
        Task UpdateCartTotalsAsync(Guid cartId, decimal totalBeforeDiscount, decimal discountAmount, decimal shippingCost, decimal finalTotal);
    }
}