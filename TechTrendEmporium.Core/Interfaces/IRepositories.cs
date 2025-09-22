using TechTrendEmporium.Core.Entities;

namespace TechTrendEmporium.Core.Interfaces;

public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> FindAsync(System.Linq.Expressions.Expression<Func<T, bool>> predicate);
    Task<T> AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(T entity);
    Task<bool> ExistsAsync(int id);
    Task<int> CountAsync();
}

public interface IUserRepository
{
    Task<User?> GetByIdAsync(string id);
    Task<User?> GetByEmailAsync(string email);
    Task<IEnumerable<User>> GetAllAsync();
    Task<User> CreateAsync(User user, string password);
    Task UpdateAsync(User user);
    Task DeleteAsync(string id);
    Task<bool> ValidatePasswordAsync(User user, string password);
    Task<bool> IsInRoleAsync(User user, string role);
    Task AddToRoleAsync(User user, string role);
    Task RemoveFromRoleAsync(User user, string role);
}

public interface IProductRepository : IRepository<Product>
{
    Task<IEnumerable<Product>> GetByCategoryAsync(int categoryId);
    Task<IEnumerable<Product>> SearchAsync(string searchTerm);
    Task<IEnumerable<Product>> GetFeaturedAsync();
    Task<Product?> GetByExternalIdAsync(int externalId);
}

public interface ICategoryRepository : IRepository<Category>
{
    Task<Category?> GetByNameAsync(string name);
    Task<Category?> GetByExternalIdAsync(int externalId);
}

public interface ICartRepository : IRepository<Cart>
{
    Task<Cart?> GetActiveCartByUserIdAsync(string userId);
    Task<IEnumerable<CartItem>> GetCartItemsAsync(int cartId);
    Task AddItemAsync(CartItem item);
    Task UpdateItemAsync(CartItem item);
    Task RemoveItemAsync(int itemId);
    Task ClearCartAsync(int cartId);
}

public interface IOrderRepository : IRepository<Order>
{
    Task<IEnumerable<Order>> GetByUserIdAsync(string userId);
    Task<IEnumerable<Order>> GetByStatusAsync(OrderStatus status);
    Task<Order?> GetWithItemsAsync(int orderId);
}

public interface IReviewRepository : IRepository<Review>
{
    Task<IEnumerable<Review>> GetByProductIdAsync(int productId);
    Task<IEnumerable<Review>> GetByUserIdAsync(string userId);
    Task<double> GetAverageRatingAsync(int productId);
    Task<IEnumerable<Review>> GetPendingApprovalsAsync();
}

public interface IWishlistRepository
{
    Task<IEnumerable<WishlistItem>> GetByUserIdAsync(string userId);
    Task<WishlistItem?> GetItemAsync(string userId, int productId);
    Task AddItemAsync(WishlistItem item);
    Task RemoveItemAsync(string userId, int productId);
    Task<bool> IsInWishlistAsync(string userId, int productId);
}

public interface ICouponRepository : IRepository<Coupon>
{
    Task<Coupon?> GetByCodeAsync(string code);
    Task<bool> IsValidAsync(string code, decimal orderAmount);
}

public interface IUnitOfWork : IDisposable
{
    IProductRepository Products { get; }
    ICategoryRepository Categories { get; }
    ICartRepository Carts { get; }
    IOrderRepository Orders { get; }
    IReviewRepository Reviews { get; }
    IWishlistRepository Wishlists { get; }
    ICouponRepository Coupons { get; }
    IUserRepository Users { get; }
    
    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}