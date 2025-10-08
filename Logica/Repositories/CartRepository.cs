using Data;
using Data.Entities;
using Data.Entities.Enums;
using Logica.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Logica.Repositories
{
    public class CartRepository : ICartRepository
    {
        private readonly AppDbContext _context;
        private readonly ILogger<CartRepository> _logger;

        public CartRepository(AppDbContext context, ILogger<CartRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Basic Cart Operations

        public async Task<Cart?> GetCartByIdAsync(Guid cartId)
        {
            return await _context.Carts
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                .Include(c => c.User)
                .Include(c => c.AppliedCoupon)
                .FirstOrDefaultAsync(c => c.Id == cartId);
        }

        public async Task<IEnumerable<Cart>> GetAllCartsAsync()
        {
            return await _context.Carts
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                .Include(c => c.User)
                .Include(c => c.AppliedCoupon)
                .ToListAsync();
        }

        public async Task<IEnumerable<Cart>> GetCartsByUserIdAsync(Guid userId)
        {
            return await _context.Carts
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                .Include(c => c.AppliedCoupon)
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<Cart> CreateCartAsync(Cart cart)
        {
            _context.Carts.Add(cart);
            await _context.SaveChangesAsync();
            return await GetCartByIdAsync(cart.Id) ?? cart;
        }

        public async Task<Cart> UpdateCartAsync(Cart cart)
        {
            cart.UpdatedAt = DateTime.UtcNow;
            
            // Si el carrito tiene items, necesitamos manejar los CartItems separadamente
            // porque EF Core puede tener problemas con las relaciones
            if (cart.CartItems?.Any() == true)
            {
                // Obtener el carrito existente con sus items
                var existingCart = await _context.Carts
                    .Include(c => c.CartItems)
                    .FirstOrDefaultAsync(c => c.Id == cart.Id);

                if (existingCart != null)
                {
                    // Eliminar items existentes
                    _context.CartItems.RemoveRange(existingCart.CartItems);
                    
                    // Actualizar propiedades del carrito
                    existingCart.Status = cart.Status;
                    existingCart.AppliedCouponId = cart.AppliedCouponId;
                    existingCart.TotalBeforeDiscount = cart.TotalBeforeDiscount;
                    existingCart.DiscountAmount = cart.DiscountAmount;
                    existingCart.ShippingCost = cart.ShippingCost;
                    existingCart.FinalTotal = cart.FinalTotal;
                    existingCart.UpdatedAt = cart.UpdatedAt;
                    
                    // Agregar nuevos items
                    foreach (var newItem in cart.CartItems)
                    {
                        _context.CartItems.Add(new CartItem
                        {
                            CartId = existingCart.Id,
                            ProductId = newItem.ProductId,
                            Quantity = newItem.Quantity,
                            UnitPriceSnapshot = newItem.UnitPriceSnapshot,
                            TitleSnapshot = newItem.TitleSnapshot,
                            ImageUrlSnapshot = newItem.ImageUrlSnapshot,
                            CategoryNameSnapshot = newItem.CategoryNameSnapshot,
                            CreatedAt = newItem.CreatedAt,
                            UpdatedAt = newItem.UpdatedAt
                        });
                    }
                }
            }
            else
            {
                _context.Carts.Update(cart);
            }
            
            await _context.SaveChangesAsync();
            return await GetCartByIdAsync(cart.Id) ?? cart;
        }

        public async Task<bool> DeleteCartAsync(Guid cartId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _logger.LogInformation("=== DELETING CART {CartId} ===", cartId);
                
                // 1. Buscar el carrito con sus items
                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                    .FirstOrDefaultAsync(c => c.Id == cartId);
                
                if (cart == null) 
                {
                    _logger.LogWarning("Cart {CartId} not found for deletion", cartId);
                    return false;
                }

                _logger.LogInformation("Found cart with {ItemCount} items", cart.CartItems?.Count ?? 0);

                // 2. Eliminar primero los CartItems manualmente
                if (cart.CartItems?.Any() == true)
                {
                    _logger.LogInformation("Deleting {ItemCount} cart items", cart.CartItems.Count);
                    foreach (var item in cart.CartItems.ToList())
                    {
                        _context.CartItems.Remove(item);
                    }
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Cart items deleted successfully");
                }

                // 3. Ahora eliminar el carrito
                _logger.LogInformation("Deleting cart entity");
                _context.Carts.Remove(cart);
                await _context.SaveChangesAsync();
                
                await transaction.CommitAsync();
                _logger.LogInformation("=== CART {CartId} DELETED SUCCESSFULLY ===", cartId);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "=== ERROR DELETING CART {CartId} ===", cartId);
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> SoftDeleteCartAsync(Guid cartId)
        {
            var cart = await _context.Carts.FindAsync(cartId);
            if (cart == null) return false;

            // Soft delete - change status
            cart.Status = CartStatus.Abandoned;
            cart.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        // External Mapping Operations

        public async Task<Cart?> GetCartByExternalIdAsync(string externalId, ExternalSource source)
        {
            var mapping = await _context.ExternalMappings
                .FirstOrDefaultAsync(em => em.SourceId == externalId && 
                                          em.Source == source && 
                                          em.SourceType == "CART");
            
            if (mapping == null) return null;

            return await GetCartByIdAsync(mapping.InternalId);
        }

        public async Task<bool> ExternalCartExistsAsync(string externalId, ExternalSource source)
        {
            return await _context.ExternalMappings.AnyAsync(em => 
                em.SourceId == externalId && 
                em.Source == source && 
                em.SourceType == "CART");
        }

        public async Task<ExternalMapping> CreateCartMappingAsync(string externalId, Guid localCartId, ExternalSource source, string snapshotJson)
        {
            var mapping = new ExternalMapping
            {
                SourceId = externalId,
                InternalId = localCartId,
                Source = source,
                SourceType = "CART",
                SnapshotJson = snapshotJson,
                ImportedAt = DateTime.UtcNow
            };

            _context.ExternalMappings.Add(mapping);
            await _context.SaveChangesAsync();
            return mapping;
        }
    }
}