using Data.Entities;

namespace Logica.Interfaces
{
    public interface IWishlistRepository
    {
        Task<Wishlist?> GetByUserIdAsync(Guid userId);
        Task<Wishlist> CreateAsync(Wishlist wishlist);
        Task<WishlistItem?> GetWishlistItemAsync(Guid wishlistId, Guid productId);
        Task<WishlistItem> AddItemAsync(WishlistItem item);
        Task RemoveItemAsync(WishlistItem item);
        Task<IEnumerable<WishlistItem>> GetWishlistItemsAsync(Guid wishlistId);
        Task<bool> ProductExistsInWishlistAsync(Guid userId, Guid productId);
    }
}