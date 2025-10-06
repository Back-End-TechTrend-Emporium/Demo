using Data;
using Data.Entities;
using Logica.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Logica.Repositories
{
    public class WishlistRepository : IWishlistRepository
    {
        private readonly AppDbContext _context;

        public WishlistRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Wishlist?> GetByUserIdAsync(Guid userId)
        {
            return await _context.Wishlists
                .Include(w => w.WishlistItems)
                    .ThenInclude(wi => wi.Product)
                        .ThenInclude(p => p.Category)
                .FirstOrDefaultAsync(w => w.UserId == userId);
        }

        public async Task<Wishlist> CreateAsync(Wishlist wishlist)
        {
            _context.Wishlists.Add(wishlist);
            await _context.SaveChangesAsync();
            return wishlist;
        }

        public async Task<WishlistItem?> GetWishlistItemAsync(Guid wishlistId, Guid productId)
        {
            return await _context.WishlistItems
                .FirstOrDefaultAsync(wi => wi.WishlistId == wishlistId && wi.ProductId == productId);
        }

        public async Task<WishlistItem> AddItemAsync(WishlistItem item)
        {
            _context.WishlistItems.Add(item);
            await _context.SaveChangesAsync();
            return item;
        }

        public async Task RemoveItemAsync(WishlistItem item)
        {
            _context.WishlistItems.Remove(item);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<WishlistItem>> GetWishlistItemsAsync(Guid wishlistId)
        {
            return await _context.WishlistItems
                .Include(wi => wi.Product)
                    .ThenInclude(p => p.Category)
                .Where(wi => wi.WishlistId == wishlistId)
                .OrderByDescending(wi => wi.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> ProductExistsInWishlistAsync(Guid userId, Guid productId)
        {
            return await _context.WishlistItems
                .AnyAsync(wi => wi.Wishlist.UserId == userId && wi.ProductId == productId);
        }
    }
}