using Logica.Models;
using Logica.Models.Carts;

namespace Logica.Interfaces
{
    public interface ICartService
    {
        // === Sync Operations ===
        Task<CartSyncResultDto> SyncCartFromFakeStoreAsync(int fakeStoreCartId, Guid createdBy);
        Task<CartSyncBatchResultDto> SyncAllCartsFromFakeStoreAsync(Guid createdBy);
        Task<CartDto?> ImportCartFromFakeStoreAsync(int fakeStoreCartId, Guid targetUserId, Guid createdBy);
    }
}