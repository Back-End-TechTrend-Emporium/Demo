using Data.Entities;
using Data.Entities.Enums;
using External.FakeStore;
using External.FakeStore.Models;
using Logica.Interfaces;
using Logica.Mappers;
using Logica.Models;
using Logica.Models.Carts;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Logica.Services
{
    public class CartService : ICartService
    {
        private readonly IFakeStoreApiService _fakeStoreApiService;
        private readonly IExternalMappingRepository _externalMappingRepository;
        private readonly ICartRepository _cartRepository;
        private readonly ILogger<CartService> _logger;

        public CartService(
            IFakeStoreApiService fakeStoreApiService,
            IExternalMappingRepository externalMappingRepository,
            ICartRepository cartRepository,
            ILogger<CartService> logger)
        {
            _fakeStoreApiService = fakeStoreApiService ?? throw new ArgumentNullException(nameof(fakeStoreApiService));
            _externalMappingRepository = externalMappingRepository ?? throw new ArgumentNullException(nameof(externalMappingRepository));
            _cartRepository = cartRepository ?? throw new ArgumentNullException(nameof(cartRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // === Sync Operations ===

        public async Task<CartSyncResultDto> SyncCartFromFakeStoreAsync(int fakeStoreCartId, Guid createdBy)
        {
            var result = new CartSyncResultDto
            {
                FakeStoreCartId = fakeStoreCartId
            };

            try
            {
                _logger.LogInformation("=== INICIO SINCRONIZACIÓN ===");
                _logger.LogInformation("Iniciando sincronización del cart {CartId} desde FakeStore", fakeStoreCartId);

                // 1. Verificar si ya existe en la BD local
                _logger.LogInformation("Paso 1: Verificando si cart ya existe en BD local");
                var existingCart = await _cartRepository.GetCartByExternalIdAsync(fakeStoreCartId.ToString(), ExternalSource.FakeStore);
                if (existingCart != null)
                {
                    _logger.LogInformation("Cart {CartId} ya existe en BD local", fakeStoreCartId);
                    result.Success = false;
                    result.Message = $"Cart {fakeStoreCartId} ya existe en la base de datos local con ID {existingCart.Id}";
                    result.LocalCartId = existingCart.Id;
                    return result;
                }

                // 2. Obtener cart desde FakeStore
                _logger.LogInformation("Paso 2: Obteniendo cart desde FakeStore API");
                var fakeStoreCart = await _fakeStoreApiService.GetCartByIdAsync(fakeStoreCartId);
                if (fakeStoreCart == null)
                {
                    _logger.LogWarning("Cart {CartId} no encontrado en FakeStore API", fakeStoreCartId);
                    result.Success = false;
                    result.Message = $"Cart {fakeStoreCartId} no encontrado en FakeStore API";
                    return result;
                }

                _logger.LogInformation("Cart obtenido desde FakeStore: UserId={UserId}, ProductCount={ProductCount}", 
                    fakeStoreCart.UserId, fakeStoreCart.Products?.Count ?? 0);

                // 3. Validar que los productos existen en la BD local
                _logger.LogInformation("Paso 3: Validando productos en BD local");
                var productIds = fakeStoreCart.Products?.Select(p => p.ProductId).ToList() ?? new List<int>();
                if (productIds.Any())
                {
                    _logger.LogInformation("Productos a validar: {ProductIds}", string.Join(", ", productIds));
                    
                    var productMappings = await MapFakeStoreProductIdsToLocalAsync(productIds);
                    var invalidIds = productIds.Where(id => !productMappings.ContainsKey(id)).ToList();
                    
                    if (invalidIds.Any())
                    {
                        _logger.LogWarning("Productos no encontrados en BD local: {InvalidIds}", string.Join(", ", invalidIds));
                        result.Success = false;
                        result.Message = $"Los siguientes productos no existen en la BD local: {string.Join(", ", invalidIds)}";
                        result.InvalidProductIds = invalidIds;
                        return result;
                    }

                    _logger.LogInformation("Todos los productos existen en BD local");

                    // 4. Crear cart local
                    _logger.LogInformation("Paso 4: Creando cart local");
                    var localCart = await CreateLocalCartFromFakeStore(fakeStoreCart, productMappings, createdBy);
                    
                    // 5. Crear mapeo externo
                    _logger.LogInformation("Paso 5: Creando mapeo externo");
                    var snapshot = JsonSerializer.Serialize(fakeStoreCart);
                    await _cartRepository.CreateCartMappingAsync(fakeStoreCartId.ToString(), localCart.Id, ExternalSource.FakeStore, snapshot);

                    result.Success = true;
                    result.Message = "Cart sincronizado exitosamente";
                    result.LocalCartId = localCart.Id;
                    result.ProductsSynced = productIds.Count;

                    _logger.LogInformation("=== SINCRONIZACIÓN EXITOSA ===");
                    _logger.LogInformation("Cart {FakeStoreCartId} sincronizado exitosamente como {LocalCartId}", 
                        fakeStoreCartId, localCart.Id);
                }
                else
                {
                    _logger.LogWarning("Cart {CartId} está vacío", fakeStoreCartId);
                    result.Success = false;
                    result.Message = "Cart vacío, no se puede sincronizar";
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "=== ERROR EN SINCRONIZACIÓN ===");
                _logger.LogError(ex, "Error sincronizando cart {CartId} desde FakeStore. Detalles: {Message}", fakeStoreCartId, ex.Message);
                _logger.LogError("Stack trace: {StackTrace}", ex.StackTrace);
                
                result.Success = false;
                result.Message = $"Error interno: {ex.Message}";
                result.Errors.Add(ex.Message);
                
                if (ex.InnerException != null)
                {
                    _logger.LogError("Inner exception: {InnerMessage}", ex.InnerException.Message);
                    result.Errors.Add($"Inner: {ex.InnerException.Message}");
                }
                
                return result;
            }
        }

        public async Task<CartSyncBatchResultDto> SyncAllCartsFromFakeStoreAsync(Guid createdBy)
        {
            var batchResult = new CartSyncBatchResultDto();

            try
            {
                _logger.LogInformation("Iniciando sincronización masiva de carts desde FakeStore API");

                // 1. Obtener todos los carts de FakeStore
                var fakeStoreCarts = await _fakeStoreApiService.GetCartsAsync();
                var cartsList = fakeStoreCarts.ToList();

                batchResult.TotalCartsProcessed = cartsList.Count;

                // 2. Sincronizar cada cart
                foreach (var fakeStoreCart in cartsList)
                {
                    var syncResult = await SyncCartFromFakeStoreAsync(fakeStoreCart.Id, createdBy);
                    batchResult.Results.Add(syncResult);

                    if (syncResult.Success)
                    {
                        batchResult.CartsSuccessful++;
                    }
                    else
                    {
                        batchResult.CartsFailed++;
                    }
                }

                batchResult.Success = batchResult.CartsSuccessful > 0;
                batchResult.Message = $"Sincronización completada: {batchResult.CartsSuccessful} exitosos, {batchResult.CartsFailed} fallidos";

                _logger.LogInformation("Sincronización masiva completada: {Successful}/{Total} carts sincronizados", 
                    batchResult.CartsSuccessful, batchResult.TotalCartsProcessed);

                return batchResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en sincronización masiva de carts desde FakeStore");
                batchResult.Success = false;
                batchResult.Message = $"Error interno en sincronización masiva: {ex.Message}";
                return batchResult;
            }
        }

        public async Task<CartDto?> ImportCartFromFakeStoreAsync(int fakeStoreCartId, Guid targetUserId, Guid createdBy)
        {
            try
            {
                _logger.LogInformation("Importando cart {CartId} desde FakeStore para usuario {UserId}", fakeStoreCartId, targetUserId);

                // 1. Obtener cart desde FakeStore
                var fakeStoreCart = await _fakeStoreApiService.GetCartByIdAsync(fakeStoreCartId);
                if (fakeStoreCart == null)
                {
                    _logger.LogWarning("Cart {CartId} no encontrado en FakeStore", fakeStoreCartId);
                    return null;
                }

                // 2. Validar productos
                var productIds = fakeStoreCart.Products?.Select(p => p.ProductId).ToList() ?? new List<int>();
                var productMappings = await MapFakeStoreProductIdsToLocalAsync(productIds);
                
                if (productIds.Count != productMappings.Count)
                {
                    throw new InvalidOperationException("Algunos productos no existen en la base de datos local");
                }

                // 3. Crear cart para el usuario específico (no crear mapeo externo)
                var localCart = await CreateLocalCartFromFakeStore(fakeStoreCart, productMappings, createdBy, targetUserId);

                _logger.LogInformation("Cart {FakeStoreCartId} importado exitosamente como {LocalCartId} para usuario {UserId}", 
                    fakeStoreCartId, localCart.Id, targetUserId);

                return localCart.ToCartDto();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importando cart {CartId} desde FakeStore", fakeStoreCartId);
                throw;
            }
        }

        // === Helper Methods ===

        private async Task<Dictionary<int, Guid>> MapFakeStoreProductIdsToLocalAsync(IEnumerable<int> fakeStoreProductIds)
        {
            try
            {
                if (!fakeStoreProductIds?.Any() == true)
                {
                    _logger.LogInformation("No hay productos para mapear");
                    return new Dictionary<int, Guid>();
                }

                _logger.LogInformation("Mapeando {Count} IDs de productos de FakeStore a IDs locales", 
                    fakeStoreProductIds.Count());
                _logger.LogDebug("IDs a mapear: {ProductIds}", string.Join(", ", fakeStoreProductIds));

                var sourceIds = fakeStoreProductIds.Select(id => id.ToString()).ToList();
                
                _logger.LogDebug("Consultando mappings para sourceIds: {SourceIds}", string.Join(", ", sourceIds));
                var mappings = await _externalMappingRepository.GetInternalIdMappingsAsync(
                    sourceIds, ExternalSource.FakeStore, "PRODUCT");

                _logger.LogInformation("Mappings encontrados: {MappingCount}", mappings.Count);
                foreach (var mapping in mappings)
                {
                    _logger.LogDebug("Mapping: {SourceId} -> {InternalId}", mapping.Key, mapping.Value);
                }

                var result = new Dictionary<int, Guid>();
                foreach (var mapping in mappings)
                {
                    if (int.TryParse(mapping.Key, out var fakeStoreId))
                    {
                        result[fakeStoreId] = mapping.Value;
                        _logger.LogDebug("Agregado al resultado: {FakeStoreId} -> {LocalId}", fakeStoreId, mapping.Value);
                    }
                    else
                    {
                        _logger.LogWarning("No se pudo parsear SourceId: {SourceId}", mapping.Key);
                    }
                }

                _logger.LogInformation("Se mapearon {MappedCount} de {RequestedCount} productos", 
                    result.Count, fakeStoreProductIds.Count());

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error mapeando IDs de productos. Detalles: {Message}", ex.Message);
                _logger.LogError("Stack trace: {StackTrace}", ex.StackTrace);
                throw new InvalidOperationException("Error interno mapeando productos.", ex);
            }
        }

        private async Task<Cart> CreateLocalCartFromFakeStore(
            FakeStoreCartResponse fakeStoreCart, 
            Dictionary<int, Guid> productMappings, 
            Guid createdBy, 
            Guid? specificUserId = null)
        {
            // Usar el usuario del sistema por defecto (el que se crea en Program.cs)
            var systemUserId = new Guid("00000000-0000-0000-0000-000000000001");
            var finalUserId = specificUserId ?? systemUserId;
            
            _logger.LogInformation("Creando cart local para usuario: {UserId} (FakeStore UserId original: {FakeStoreUserId})", 
                finalUserId, fakeStoreCart.UserId);

            // Crear cart local
            var localCart = new Cart
            {
                UserId = finalUserId,
                Status = CartStatus.Active,
                CreatedAt = fakeStoreCart.Date,
                UpdatedAt = DateTime.UtcNow
            };

            // Agregar items del cart
            foreach (var fakeStoreProduct in fakeStoreCart.Products ?? new List<FakeStoreCartProduct>())
            {
                if (productMappings.TryGetValue(fakeStoreProduct.ProductId, out var localProductId))
                {
                    var cartItem = new CartItem
                    {
                        CartId = localCart.Id,
                        ProductId = localProductId,
                        Quantity = fakeStoreProduct.Quantity,
                        UnitPriceSnapshot = 0, // TODO: obtener precio del producto local
                        CreatedAt = DateTime.UtcNow
                    };

                    localCart.CartItems.Add(cartItem);
                    _logger.LogDebug("Agregado item: Producto {ProductId}, Cantidad {Quantity}", 
                        localProductId, fakeStoreProduct.Quantity);
                }
                else
                {
                    _logger.LogWarning("Producto FakeStore {ProductId} no encontrado en mappings", fakeStoreProduct.ProductId);
                }
            }

            // Calcular totales (simplificado)
            localCart.TotalBeforeDiscount = localCart.CartItems.Sum(ci => ci.UnitPriceSnapshot * ci.Quantity);
            localCart.FinalTotal = localCart.TotalBeforeDiscount;

            _logger.LogInformation("Cart local creado con {ItemCount} items, Total: {Total}", 
                localCart.CartItems.Count, localCart.FinalTotal);

            return await _cartRepository.CreateCartAsync(localCart);
        }

        private static Guid ConvertIntToGuid(int id)
        {
            var bytes = new byte[16];
            var idBytes = BitConverter.GetBytes(id);
            Array.Copy(idBytes, 0, bytes, 0, 4);
            return new Guid(bytes);
        }
    }
}