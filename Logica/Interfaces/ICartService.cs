using Logica.Models;
using Logica.Models.Carts;
using Logica.Mappers;

namespace Logica.Interfaces
{
    public interface ICartService
    {
        // === OPERACIONES CENTRADAS EN USUARIO (LÓGICA REAL) ===
        
        /// <summary>
        /// Obtiene todos los carritos de un usuario específico
        /// </summary>
        Task<IEnumerable<CartDto>> GetCartsByUserIdAsync(Guid userId);
        
        /// <summary>
        /// Obtiene el carrito activo del usuario (o null si no tiene)
        /// </summary>
        Task<CartDto?> GetActiveCartByUserIdAsync(Guid userId);
        
        /// <summary>
        /// Crea un carrito vacío para el usuario
        /// </summary>
        Task<CartDto> CreateEmptyCartForUserAsync(Guid userId);
        
        /// <summary>
        /// Agrega un item al carrito activo del usuario
        /// </summary>
        Task<CartDto> AddItemToUserCartAsync(Guid userId, AddItemToCartRequest request);
        
        /// <summary>
        /// Actualiza la cantidad de un item en el carrito del usuario
        /// </summary>
        Task<CartDto> UpdateItemInUserCartAsync(Guid userId, UpdateCartItemQuantityRequest request);
        
        /// <summary>
        /// Remueve un item del carrito del usuario
        /// </summary>
        Task<CartDto> RemoveItemFromUserCartAsync(Guid userId, Guid productId);
        
        /// <summary>
        /// Limpia todo el carrito del usuario
        /// </summary>
        Task<CartDto> ClearUserCartAsync(Guid userId);
        
        /// <summary>
        /// Marca el carrito como comprado (checkout)
        /// </summary>
        Task<CartDto> CheckoutUserCartAsync(Guid userId);

        /// <summary>
        /// Restaura inventario de un carrito (para cancelaciones)
        /// </summary>
        Task RestoreInventoryAsync(Guid cartId);

        /// <summary>
        /// Obtiene warnings de inventario para el carrito sin bloquear operaciones
        /// </summary>
        Task<IEnumerable<string>> GetInventoryWarningsAsync(Guid userId);

        // === OPERACIONES ADMINISTRATIVAS (ORIGINAL) ===
        
        /// <summary>
        /// Obtiene información completa de un carrito específico (para administradores)
        /// </summary>
        Task<CartFullDetailsDto?> GetCartFullDetailsByIdAsync(Guid cartId);
        
        /// <summary>
        /// Obtiene todos los carritos con información completa
        /// </summary>
        Task<IEnumerable<CartFullDetailsDto>> GetAllCartsFullDetailsAsync();
        
        /// <summary>
        /// Obtiene carritos por usuario con información completa
        /// </summary>
        Task<IEnumerable<CartFullDetailsDto>> GetCartsByUserFullDetailsAsync(Guid userId);
        
        /// <summary>
        /// Obtiene resumen dashboard para administradores
        /// </summary>
        Task<CartsDashboardSummaryDto> GetCartsDashboardSummaryAsync();
        
        /// <summary>
        /// Obtiene carritos filtrados por estado con información completa
        /// </summary>
        Task<IEnumerable<CartFullDetailsDto>> GetCartsByStatusFullDetailsAsync(Data.Entities.Enums.CartStatus status);

        // === OPERACIONES FAKESTORE (SOLO ADMIN) ===
        
        /// <summary>
        /// Obtiene carritos desde FakeStore
        /// </summary>
        Task<IEnumerable<CartDto>> GetCartsFromFakeStoreAsync();
        
        /// <summary>
        /// Obtiene carrito específico desde FakeStore
        /// </summary>
        Task<CartDto?> GetCartFromFakeStoreAsync(int id);
        
        /// <summary>
        /// Obtiene carritos de usuario desde FakeStore
        /// </summary>
        Task<IEnumerable<CartDto>> GetUserCartsFromFakeStoreAsync(int userId);
        
        /// <summary>
        /// Sincroniza un carrito específico desde FakeStore
        /// </summary>
        Task<CartSyncResultDto> SyncCartFromFakeStoreAsync(int fakeStoreCartId, Guid createdBy);
        
        /// <summary>
        /// Sincroniza todos los carritos desde FakeStore
        /// </summary>
        Task<CartSyncBatchResultDto> SyncAllCartsFromFakeStoreAsync(Guid createdBy = default);
        
        /// <summary>
        /// Importa un carrito desde FakeStore
        /// </summary>
        Task<CartDto?> ImportCartFromFakeStoreAsync(int fakeStoreCartId, Guid targetUserId = default, Guid createdBy = default);
    }
}