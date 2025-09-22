using Microsoft.EntityFrameworkCore;
using TechTrendEmporium.Core.Entities;
using TechTrendEmporium.Core.Interfaces;
using TechTrendEmporium.Infrastructure.Data;

namespace TechTrendEmporium.Infrastructure.Repositories;

public class CartRepository : Repository<Cart>, ICartRepository
{
    public CartRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Cart?> GetActiveCartByUserIdAsync(string userId)
    {
        return await _dbSet
            .Include(c => c.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId && c.IsActive);
    }

    public async Task<IEnumerable<CartItem>> GetCartItemsAsync(int cartId)
    {
        return await _context.CartItems
            .Include(i => i.Product)
            .Where(i => i.CartId == cartId)
            .ToListAsync();
    }

    public async Task AddItemAsync(CartItem item)
    {
        await _context.CartItems.AddAsync(item);
    }

    public Task UpdateItemAsync(CartItem item)
    {
        _context.CartItems.Update(item);
        return Task.CompletedTask;
    }

    public Task RemoveItemAsync(int itemId)
    {
        var item = _context.CartItems.Find(itemId);
        if (item != null)
        {
            _context.CartItems.Remove(item);
        }
        return Task.CompletedTask;
    }

    public async Task ClearCartAsync(int cartId)
    {
        var items = await _context.CartItems
            .Where(i => i.CartId == cartId)
            .ToListAsync();
        
        _context.CartItems.RemoveRange(items);
    }
}

public class OrderRepository : Repository<Order>, IOrderRepository
{
    public OrderRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Order>> GetByUserIdAsync(string userId)
    {
        return await _dbSet
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Order>> GetByStatusAsync(OrderStatus status)
    {
        return await _dbSet
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
            .Include(o => o.User)
            .Where(o => o.Status == status)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();
    }

    public async Task<Order?> GetWithItemsAsync(int orderId)
    {
        return await _dbSet
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
            .Include(o => o.User)
            .FirstOrDefaultAsync(o => o.Id == orderId);
    }
}