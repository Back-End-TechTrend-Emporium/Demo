using Microsoft.EntityFrameworkCore;
using TechTrendEmporium.Core.Entities;
using TechTrendEmporium.Core.Interfaces;
using TechTrendEmporium.Infrastructure.Data;

namespace TechTrendEmporium.Infrastructure.Repositories;

public class ReviewRepository : Repository<Review>, IReviewRepository
{
    public ReviewRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Review>> GetByProductIdAsync(int productId)
    {
        return await _dbSet
            .Include(r => r.User)
            .Where(r => r.ProductId == productId && r.IsApproved)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Review>> GetByUserIdAsync(string userId)
    {
        return await _dbSet
            .Include(r => r.Product)
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<double> GetAverageRatingAsync(int productId)
    {
        var reviews = await _dbSet
            .Where(r => r.ProductId == productId && r.IsApproved)
            .ToListAsync();

        return reviews.Any() ? reviews.Average(r => r.Rating) : 0;
    }

    public async Task<IEnumerable<Review>> GetPendingApprovalsAsync()
    {
        return await _dbSet
            .Include(r => r.User)
            .Include(r => r.Product)
            .Where(r => !r.IsApproved)
            .OrderBy(r => r.CreatedAt)
            .ToListAsync();
    }
}

public class WishlistRepository : IWishlistRepository
{
    private readonly ApplicationDbContext _context;

    public WishlistRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<WishlistItem>> GetByUserIdAsync(string userId)
    {
        return await _context.WishlistItems
            .Include(w => w.Product)
                .ThenInclude(p => p.Category)
            .Where(w => w.UserId == userId)
            .OrderByDescending(w => w.AddedAt)
            .ToListAsync();
    }

    public async Task<WishlistItem?> GetItemAsync(string userId, int productId)
    {
        return await _context.WishlistItems
            .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);
    }

    public async Task AddItemAsync(WishlistItem item)
    {
        await _context.WishlistItems.AddAsync(item);
    }

    public async Task RemoveItemAsync(string userId, int productId)
    {
        var item = await GetItemAsync(userId, productId);
        if (item != null)
        {
            _context.WishlistItems.Remove(item);
        }
    }

    public async Task<bool> IsInWishlistAsync(string userId, int productId)
    {
        return await _context.WishlistItems
            .AnyAsync(w => w.UserId == userId && w.ProductId == productId);
    }
}

public class CouponRepository : Repository<Coupon>, ICouponRepository
{
    public CouponRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Coupon?> GetByCodeAsync(string code)
    {
        return await _dbSet
            .FirstOrDefaultAsync(c => c.Code == code && c.IsActive);
    }

    public async Task<bool> IsValidAsync(string code, decimal orderAmount)
    {
        var coupon = await GetByCodeAsync(code);
        
        if (coupon == null) return false;
        
        var now = DateTime.UtcNow;
        
        return coupon.IsActive &&
               now >= coupon.ValidFrom &&
               now <= coupon.ValidTo &&
               coupon.UsedCount < coupon.UsageLimit &&
               (coupon.MinimumOrderAmount == null || orderAmount >= coupon.MinimumOrderAmount);
    }
}