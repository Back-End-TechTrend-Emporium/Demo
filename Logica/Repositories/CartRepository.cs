using Data;
using Data.Entities;
using Data.Entities.Enums;
using Logica.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Logica.Repositories
{
    public class CartRepository : ICartRepository
    {
        private readonly AppDbContext _context;

        public CartRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Cart?> GetCartByIdAsync(Guid cartId)
        {
            return await _context.Carts
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                .Include(c => c.User)
                .Include(c => c.AppliedCoupon)
                .FirstOrDefaultAsync(c => c.Id == cartId);
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
            _context.Carts.Update(cart);
            await _context.SaveChangesAsync();
            return await GetCartByIdAsync(cart.Id) ?? cart;
        }

        public async Task<bool> DeleteCartAsync(Guid cartId)
        {
            var cart = await _context.Carts.FindAsync(cartId);
            if (cart == null) return false;

            // Soft delete - cambiar estado
            cart.Status = CartStatus.Abandoned;
            cart.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

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