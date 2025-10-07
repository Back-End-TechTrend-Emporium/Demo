using Data.Entities;
using Data.Entities.Enums;

namespace Logica.Interfaces
{
    public interface ICartRepository
    {
        // === Basic Cart Operations ===
        Task<Cart?> GetCartByIdAsync(Guid cartId);
        Task<Cart> CreateCartAsync(Cart cart);
        Task<Cart> UpdateCartAsync(Cart cart);
        Task<bool> DeleteCartAsync(Guid cartId);

        // === External Mapping Operations ===
        Task<Cart?> GetCartByExternalIdAsync(string externalId, ExternalSource source);
        Task<bool> ExternalCartExistsAsync(string externalId, ExternalSource source);
        Task<ExternalMapping> CreateCartMappingAsync(string externalId, Guid localCartId, ExternalSource source, string snapshotJson);
    }
}