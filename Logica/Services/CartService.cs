using Data.Entities.Enums;
using External.FakeStore;
using External.FakeStore.Models;
using Logica.Interfaces;
using Microsoft.Extensions.Logging;

namespace Logica.Services
{
    public class CartService : ICartService
    {
        private readonly IFakeStoreApiService _fakeStoreApiService;
        private readonly IExternalMappingRepository _externalMappingRepository;
        // private readonly ICartRepository _cartRepository; // TODO: Uncomment when CartRepository is implemented
        private readonly ILogger<CartService> _logger;

        public CartService(
            IFakeStoreApiService fakeStoreApiService,
            IExternalMappingRepository externalMappingRepository,
            // ICartRepository cartRepository, // TODO: Uncomment when CartRepository is implemented
            ILogger<CartService> logger)
        {
            _fakeStoreApiService = fakeStoreApiService ?? throw new ArgumentNullException(nameof(fakeStoreApiService));
            _externalMappingRepository = externalMappingRepository ?? throw new ArgumentNullException(nameof(externalMappingRepository));
            // _cartRepository = cartRepository ?? throw new ArgumentNullException(nameof(cartRepository)); // TODO: Uncomment when implemented
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // === FakeStore API Operations ===

        public async Task<IEnumerable<FakeStoreCartResponse>> GetCartsFromFakeStoreAsync()
        {
            try
            {
                _logger.LogInformation("Iniciando obtención de todos los carts desde FakeStore API");
                
                var fakeStoreCarts = await _fakeStoreApiService.GetCartsAsync();
                
                _logger.LogInformation("Se obtuvieron {Count} carts desde FakeStore API", fakeStoreCarts?.Count() ?? 0);
                return fakeStoreCarts ?? Enumerable.Empty<FakeStoreCartResponse>();
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "Error de conectividad al obtener carts de FakeStore API");
                throw new InvalidOperationException("Error de conectividad con FakeStore API. Verifica la conexión a internet.", httpEx);
            }
            catch (TaskCanceledException tcEx) when (tcEx.InnerException is TimeoutException)
            {
                _logger.LogError(tcEx, "Timeout al obtener carts de FakeStore API");
                throw new TimeoutException("La solicitud a FakeStore API ha excedido el tiempo límite.", tcEx);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al obtener carts de FakeStore API");
                throw new InvalidOperationException("Error interno al obtener carts de FakeStore API.", ex);
            }
        }

        public async Task<FakeStoreCartResponse?> GetCartFromFakeStoreAsync(int cartId)
        {
            try
            {
                if (cartId <= 0)
                {
                    _logger.LogWarning("Intento de obtener cart con ID inválido: {CartId}", cartId);
                    throw new ArgumentException("El ID del cart debe ser mayor a 0", nameof(cartId));
                }

                _logger.LogInformation("Obteniendo cart {CartId} desde FakeStore API", cartId);
                
                var fakeStoreCart = await _fakeStoreApiService.GetCartByIdAsync(cartId);
                
                if (fakeStoreCart == null)
                {
                    _logger.LogInformation("Cart {CartId} no encontrado en FakeStore API", cartId);
                    return null;
                }

                _logger.LogInformation("Cart {CartId} obtenido exitosamente desde FakeStore API con {ProductCount} productos", 
                    cartId, fakeStoreCart.Products?.Count ?? 0);
                return fakeStoreCart;
            }
            catch (ArgumentException)
            {
                throw; // Re-throw argument exceptions as they are
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "Error de conectividad al obtener cart {CartId} de FakeStore API", cartId);
                throw new InvalidOperationException($"Error de conectividad al obtener cart {cartId} de FakeStore API.", httpEx);
            }
            catch (TaskCanceledException tcEx) when (tcEx.InnerException is TimeoutException)
            {
                _logger.LogError(tcEx, "Timeout al obtener cart {CartId} de FakeStore API", cartId);
                throw new TimeoutException($"La solicitud para obtener cart {cartId} ha excedido el tiempo límite.", tcEx);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al obtener cart {CartId} de FakeStore API", cartId);
                throw new InvalidOperationException($"Error interno al obtener cart {cartId} de FakeStore API.", ex);
            }
        }

        public async Task<FakeStoreCartResponse?> CreateCartInFakeStoreAsync(FakeStoreCartCreateRequest cartRequest)
        {
            try
            {
                if (cartRequest == null)
                {
                    throw new ArgumentNullException(nameof(cartRequest), "La solicitud de creación de cart no puede ser nula");
                }

                if (cartRequest.UserId <= 0)
                {
                    throw new ArgumentException("El ID del usuario debe ser mayor a 0", nameof(cartRequest.UserId));
                }

                // Validar que los productos existen en la base de datos local
                if (cartRequest.Products?.Any() == true)
                {
                    var productIds = cartRequest.Products.Select(p => p.ProductId).ToList();
                    var validationResult = await ValidateProductsExistInLocalDbAsync(productIds);
                    
                    if (!validationResult)
                    {
                        var invalidIds = await GetInvalidProductIds(productIds);
                        _logger.LogWarning("Intento de crear cart con productos que no existen en la BD local: {InvalidIds}", 
                            string.Join(", ", invalidIds));
                        throw new InvalidOperationException($"Los siguientes productos de FakeStore no existen en la base de datos local: {string.Join(", ", invalidIds)}");
                    }
                }

                _logger.LogInformation("Creando nuevo cart en FakeStore API para usuario {UserId} con {ProductCount} productos", 
                    cartRequest.UserId, cartRequest.Products?.Count ?? 0);
                
                var fakeStoreCart = await _fakeStoreApiService.CreateCartAsync(cartRequest);
                
                if (fakeStoreCart == null)
                {
                    _logger.LogWarning("FakeStore API retornó null al crear cart para usuario {UserId}", cartRequest.UserId);
                    throw new InvalidOperationException("Error al crear cart en FakeStore API - respuesta nula");
                }

                _logger.LogInformation("Cart creado exitosamente en FakeStore API con ID {CartId}", fakeStoreCart.Id);
                return fakeStoreCart;
            }
            catch (ArgumentException)
            {
                throw; // Re-throw argument exceptions as they are
            }
            catch (InvalidOperationException)
            {
                throw; // Re-throw business logic exceptions as they are
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "Error de conectividad al crear cart en FakeStore API");
                throw new InvalidOperationException("Error de conectividad al crear cart en FakeStore API.", httpEx);
            }
            catch (TaskCanceledException tcEx) when (tcEx.InnerException is TimeoutException)
            {
                _logger.LogError(tcEx, "Timeout al crear cart en FakeStore API");
                throw new TimeoutException("La solicitud para crear cart ha excedido el tiempo límite.", tcEx);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al crear cart en FakeStore API");
                throw new InvalidOperationException("Error interno al crear cart en FakeStore API.", ex);
            }
        }

        public async Task<FakeStoreCartResponse?> UpdateCartInFakeStoreAsync(int cartId, FakeStoreCartUpdateRequest cartRequest)
        {
            try
            {
                if (cartId <= 0)
                {
                    throw new ArgumentException("El ID del cart debe ser mayor a 0", nameof(cartId));
                }

                if (cartRequest == null)
                {
                    throw new ArgumentNullException(nameof(cartRequest), "La solicitud de actualización de cart no puede ser nula");
                }

                if (cartRequest.UserId <= 0)
                {
                    throw new ArgumentException("El ID del usuario debe ser mayor a 0", nameof(cartRequest.UserId));
                }

                // Validar que los productos existen en la base de datos local
                if (cartRequest.Products?.Any() == true)
                {
                    var productIds = cartRequest.Products.Select(p => p.ProductId).ToList();
                    var validationResult = await ValidateProductsExistInLocalDbAsync(productIds);
                    
                    if (!validationResult)
                    {
                        var invalidIds = await GetInvalidProductIds(productIds);
                        _logger.LogWarning("Intento de actualizar cart {CartId} con productos que no existen en la BD local: {InvalidIds}", 
                            cartId, string.Join(", ", invalidIds));
                        throw new InvalidOperationException($"Los siguientes productos de FakeStore no existen en la base de datos local: {string.Join(", ", invalidIds)}");
                    }
                }

                _logger.LogInformation("Actualizando cart {CartId} en FakeStore API para usuario {UserId}", 
                    cartId, cartRequest.UserId);
                
                var fakeStoreCart = await _fakeStoreApiService.UpdateCartAsync(cartId, cartRequest);
                
                if (fakeStoreCart == null)
                {
                    _logger.LogWarning("Cart {CartId} no encontrado al intentar actualizar en FakeStore API", cartId);
                    return null;
                }

                _logger.LogInformation("Cart {CartId} actualizado exitosamente en FakeStore API", cartId);
                return fakeStoreCart;
            }
            catch (ArgumentException)
            {
                throw; // Re-throw argument exceptions as they are
            }
            catch (InvalidOperationException)
            {
                throw; // Re-throw business logic exceptions as they are
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "Error de conectividad al actualizar cart {CartId} en FakeStore API", cartId);
                throw new InvalidOperationException($"Error de conectividad al actualizar cart {cartId} en FakeStore API.", httpEx);
            }
            catch (TaskCanceledException tcEx) when (tcEx.InnerException is TimeoutException)
            {
                _logger.LogError(tcEx, "Timeout al actualizar cart {CartId} en FakeStore API", cartId);
                throw new TimeoutException($"La solicitud para actualizar cart {cartId} ha excedido el tiempo límite.", tcEx);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al actualizar cart {CartId} en FakeStore API", cartId);
                throw new InvalidOperationException($"Error interno al actualizar cart {cartId} en FakeStore API.", ex);
            }
        }

        public async Task<FakeStoreCartResponse?> DeleteCartInFakeStoreAsync(int cartId)
        {
            try
            {
                if (cartId <= 0)
                {
                    throw new ArgumentException("El ID del cart debe ser mayor a 0", nameof(cartId));
                }

                _logger.LogInformation("Eliminando cart {CartId} en FakeStore API", cartId);
                
                var fakeStoreCart = await _fakeStoreApiService.DeleteCartAsync(cartId);
                
                if (fakeStoreCart == null)
                {
                    _logger.LogWarning("Cart {CartId} no encontrado al intentar eliminar en FakeStore API", cartId);
                    return null;
                }

                _logger.LogInformation("Cart {CartId} eliminado exitosamente en FakeStore API", cartId);
                return fakeStoreCart;
            }
            catch (ArgumentException)
            {
                throw; // Re-throw argument exceptions as they are
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "Error de conectividad al eliminar cart {CartId} en FakeStore API", cartId);
                throw new InvalidOperationException($"Error de conectividad al eliminar cart {cartId} en FakeStore API.", httpEx);
            }
            catch (TaskCanceledException tcEx) when (tcEx.InnerException is TimeoutException)
            {
                _logger.LogError(tcEx, "Timeout al eliminar cart {CartId} en FakeStore API", cartId);
                throw new TimeoutException($"La solicitud para eliminar cart {cartId} ha excedido el tiempo límite.", tcEx);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al eliminar cart {CartId} en FakeStore API", cartId);
                throw new InvalidOperationException($"Error interno al eliminar cart {cartId} en FakeStore API.", ex);
            }
        }

        // === Product Validation for FakeStore Operations ===

        public async Task<bool> ValidateProductsExistInLocalDbAsync(IEnumerable<int> fakeStoreProductIds)
        {
            try
            {
                if (!fakeStoreProductIds?.Any() == true)
                {
                    return true; // Cart vacío es válido
                }

                _logger.LogInformation("Validando existencia de {Count} productos de FakeStore en la BD local", 
                    fakeStoreProductIds.Count());

                var sourceIds = fakeStoreProductIds.Select(id => id.ToString()).ToList();
                var mappings = await _externalMappingRepository.GetInternalIdMappingsAsync(
                    sourceIds, ExternalSource.FakeStore, "PRODUCT");

                var existingCount = mappings.Count;
                var requestedCount = sourceIds.Count;

                _logger.LogInformation("Se encontraron {ExistingCount} de {RequestedCount} productos en la BD local", 
                    existingCount, requestedCount);

                return existingCount == requestedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validando productos en la BD local");
                throw new InvalidOperationException("Error interno validando productos.", ex);
            }
        }

        public async Task<Dictionary<int, Guid>> MapFakeStoreProductIdsToLocalAsync(IEnumerable<int> fakeStoreProductIds)
        {
            try
            {
                if (!fakeStoreProductIds?.Any() == true)
                {
                    return new Dictionary<int, Guid>();
                }

                _logger.LogInformation("Mapeando {Count} IDs de productos de FakeStore a IDs locales", 
                    fakeStoreProductIds.Count());

                var sourceIds = fakeStoreProductIds.Select(id => id.ToString()).ToList();
                var mappings = await _externalMappingRepository.GetInternalIdMappingsAsync(
                    sourceIds, ExternalSource.FakeStore, "PRODUCT");

                var result = new Dictionary<int, Guid>();
                foreach (var mapping in mappings)
                {
                    if (int.TryParse(mapping.Key, out var fakeStoreId))
                    {
                        result[fakeStoreId] = mapping.Value;
                    }
                }

                _logger.LogInformation("Se mapearon {MappedCount} de {RequestedCount} productos", 
                    result.Count, fakeStoreProductIds.Count());

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error mapeando IDs de productos");
                throw new InvalidOperationException("Error interno mapeando productos.", ex);
            }
        }

        // === Private Helper Methods ===

        private async Task<IEnumerable<int>> GetInvalidProductIds(IEnumerable<int> fakeStoreProductIds)
        {
            try
            {
                var sourceIds = fakeStoreProductIds.Select(id => id.ToString()).ToList();
                var mappings = await _externalMappingRepository.GetInternalIdMappingsAsync(
                    sourceIds, ExternalSource.FakeStore, "PRODUCT");

                var existingFakeStoreIds = mappings.Keys.Select(int.Parse).ToHashSet();
                return fakeStoreProductIds.Where(id => !existingFakeStoreIds.Contains(id));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo IDs de productos inválidos");
                return fakeStoreProductIds; // Return all as invalid if we can't determine
            }
        }

        // === TODO: Local Cart Operations - Implement when CartRepository is ready ===
        // (Los métodos comentados permanecen igual)
    }
}