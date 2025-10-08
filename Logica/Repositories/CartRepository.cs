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
            try
            {
                _logger.LogInformation("?? Updating cart {CartId}", cart.Id);
                
                cart.UpdatedAt = DateTime.UtcNow;
                
                // Re-obtener el carrito actual desde la BD SIN transacciones manuales
                var currentCart = await _context.Carts
                    .Include(c => c.CartItems)
                    .FirstOrDefaultAsync(c => c.Id == cart.Id);

                if (currentCart == null)
                {
                    throw new InvalidOperationException($"Cart {cart.Id} not found");
                }

                // Actualizar propiedades del carrito sin tocar los items aún
                currentCart.Status = cart.Status;
                currentCart.AppliedCouponId = cart.AppliedCouponId;
                currentCart.TotalBeforeDiscount = cart.TotalBeforeDiscount;
                currentCart.DiscountAmount = cart.DiscountAmount;
                currentCart.ShippingCost = cart.ShippingCost;
                currentCart.FinalTotal = cart.FinalTotal;
                currentCart.UpdatedAt = cart.UpdatedAt;

                // Si hay items nuevos que agregar/actualizar
                if (cart.CartItems?.Any() == true)
                {
                    // ? USAR TOLIST() PARA EVITAR "COLLECTION MODIFIED" ERROR
                    // Eliminar todos los items existentes
                    if (currentCart.CartItems?.Any() == true)
                    {
                        var itemsToRemove = currentCart.CartItems.ToList(); // ? FIX AQUÍ
                        _context.CartItems.RemoveRange(itemsToRemove);
                    }

                    // Agregar los nuevos items
                    var itemsToAdd = cart.CartItems.ToList(); // ? FIX AQUÍ
                    foreach (var newItem in itemsToAdd)
                    {
                        var cartItem = new CartItem
                        {
                            CartId = currentCart.Id,
                            ProductId = newItem.ProductId,
                            Quantity = newItem.Quantity,
                            UnitPriceSnapshot = newItem.UnitPriceSnapshot,
                            TitleSnapshot = newItem.TitleSnapshot,
                            ImageUrlSnapshot = newItem.ImageUrlSnapshot,
                            CategoryNameSnapshot = newItem.CategoryNameSnapshot,
                            CreatedAt = newItem.CreatedAt,
                            UpdatedAt = newItem.UpdatedAt
                        };
                        _context.CartItems.Add(cartItem);
                    }
                }

                // Guardar todos los cambios SIN transacciones manuales
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("? Cart {CartId} updated successfully", cart.Id);
                
                // Retornar el carrito actualizado
                return await GetCartByIdAsync(cart.Id) ?? currentCart;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "?? Error updating cart {CartId}", cart.Id);
                throw;
            }
        }

        public async Task<bool> DeleteCartAsync(Guid cartId)
        {
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

                // 2. Con cascade delete configurado, solo necesitamos eliminar el cart
                // Pero por seguridad, eliminamos items primero usando RemoveRange
                if (cart.CartItems?.Any() == true)
                {
                    // ? USAR TOLIST() PARA EVITAR "COLLECTION MODIFIED" ERROR
                    var itemsToRemove = cart.CartItems.ToList(); // ? FIX AQUÍ
                    _logger.LogInformation("Removing {ItemCount} cart items using RemoveRange", itemsToRemove.Count);
                    _context.CartItems.RemoveRange(itemsToRemove);
                }

                // 3. Eliminar el carrito
                _logger.LogInformation("Removing cart entity");
                _context.Carts.Remove(cart);
                
                // 4. Guardar todos los cambios de una vez
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("=== CART {CartId} DELETED SUCCESSFULLY ===", cartId);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "=== ERROR DELETING CART {CartId} ===", cartId);
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

        public async Task<Cart?> GetActiveCartByUserIdAsync(Guid userId)
        {
            return await _context.Carts
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                        .ThenInclude(p => p.Category)
                .Include(c => c.AppliedCoupon)
                .Where(c => c.UserId == userId && c.Status == CartStatus.Active)
                .OrderByDescending(c => c.UpdatedAt)
                .FirstOrDefaultAsync();
        }

        // Direct cart item operations to avoid concurrency issues
        public async Task<CartItem> AddOrUpdateCartItemAsync(Guid cartId, Guid productId, int quantity, decimal unitPrice, string? title = null, string? imageUrl = null, string? categoryName = null)
        {
            try
            {
                _logger.LogInformation("?? Adding/updating cart item: Cart={CartId}, Product={ProductId}, Quantity={Quantity}", 
                    cartId, productId, quantity);

                // Buscar item existente
                var existingItem = await _context.CartItems
                    .FirstOrDefaultAsync(ci => ci.CartId == cartId && ci.ProductId == productId);

                if (existingItem != null)
                {
                    // Actualizar item existente
                    existingItem.Quantity = quantity;
                    existingItem.UpdatedAt = DateTime.UtcNow;
                    _logger.LogInformation("?? Updated existing cart item quantity to {Quantity}", quantity);
                }
                else
                {
                    // Crear nuevo item
                    existingItem = new CartItem
                    {
                        CartId = cartId,
                        ProductId = productId,
                        Quantity = quantity,
                        UnitPriceSnapshot = unitPrice,
                        TitleSnapshot = title,
                        ImageUrlSnapshot = imageUrl,
                        CategoryNameSnapshot = categoryName,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.CartItems.Add(existingItem);
                    _logger.LogInformation("? Created new cart item");
                }

                await _context.SaveChangesAsync();
                return existingItem;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "?? Error adding/updating cart item");
                throw;
            }
        }

        public async Task<bool> RemoveCartItemAsync(Guid cartId, Guid productId)
        {
            try
            {
                var item = await _context.CartItems
                    .FirstOrDefaultAsync(ci => ci.CartId == cartId && ci.ProductId == productId);

                if (item == null)
                    return false;

                _context.CartItems.Remove(item);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("??? Removed cart item: Cart={CartId}, Product={ProductId}", cartId, productId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "?? Error removing cart item");
                throw;
            }
        }

        public async Task UpdateCartTotalsAsync(Guid cartId, decimal totalBeforeDiscount, decimal discountAmount, decimal shippingCost, decimal finalTotal)
        {
            try
            {
                var cart = await _context.Carts.FindAsync(cartId);
                if (cart == null)
                    throw new InvalidOperationException($"Cart {cartId} not found");

                cart.TotalBeforeDiscount = totalBeforeDiscount;
                cart.DiscountAmount = discountAmount;
                cart.ShippingCost = shippingCost;
                cart.FinalTotal = finalTotal;
                cart.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                
                _logger.LogInformation("?? Updated cart totals: Cart={CartId}, Total={FinalTotal}", cartId, finalTotal);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "?? Error updating cart totals");
                throw;
            }
        }
    }
}