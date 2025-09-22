using Microsoft.EntityFrameworkCore.Storage;
using TechTrendEmporium.Core.Interfaces;
using TechTrendEmporium.Infrastructure.Data;

namespace TechTrendEmporium.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private IDbContextTransaction? _transaction;

    public UnitOfWork(
        ApplicationDbContext context,
        IProductRepository products,
        ICategoryRepository categories,
        ICartRepository carts,
        IOrderRepository orders,
        IReviewRepository reviews,
        IWishlistRepository wishlists,
        ICouponRepository coupons,
        IUserRepository users)
    {
        _context = context;
        Products = products;
        Categories = categories;
        Carts = carts;
        Orders = orders;
        Reviews = reviews;
        Wishlists = wishlists;
        Coupons = coupons;
        Users = users;
    }

    public IProductRepository Products { get; }
    public ICategoryRepository Categories { get; }
    public ICartRepository Carts { get; }
    public IOrderRepository Orders { get; }
    public IReviewRepository Reviews { get; }
    public IWishlistRepository Wishlists { get; }
    public ICouponRepository Coupons { get; }
    public IUserRepository Users { get; }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public async Task BeginTransactionAsync()
    {
        _transaction = await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}