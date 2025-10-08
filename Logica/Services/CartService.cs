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
                _logger.LogInformation("=== SYNC START ===");
                _logger.LogInformation("Starting cart {CartId} sync from FakeStore", fakeStoreCartId);

                // 1. Check if already exists in local DB
                _logger.LogInformation("Step 1: Checking if cart already exists in local DB");
                var existingCart = await _cartRepository.GetCartByExternalIdAsync(fakeStoreCartId.ToString(), ExternalSource.FakeStore);
                if (existingCart != null)
                {
                    _logger.LogInformation("Cart {CartId} already exists in local DB", fakeStoreCartId);
                    result.Success = false;
                    result.Message = $"Cart {fakeStoreCartId} already exists in local database with ID {existingCart.Id}";
                    result.LocalCartId = existingCart.Id;
                    return result;
                }

                // 2. Get cart from FakeStore
                _logger.LogInformation("Step 2: Getting cart from FakeStore API");
                var fakeStoreCart = await _fakeStoreApiService.GetCartByIdAsync(fakeStoreCartId);
                if (fakeStoreCart == null)
                {
                    _logger.LogWarning("Cart {CartId} not found in FakeStore API", fakeStoreCartId);
                    result.Success = false;
                    result.Message = $"Cart {fakeStoreCartId} not found in FakeStore API";
                    return result;
                }

                _logger.LogInformation("Cart obtained from FakeStore: UserId={UserId}, ProductCount={ProductCount}", 
                    fakeStoreCart.UserId, fakeStoreCart.Products?.Count ?? 0);

                // 3. Validate that products exist in local DB
                _logger.LogInformation("Step 3: Validating products in local DB");
                var productIds = fakeStoreCart.Products?.Select(p => p.ProductId).ToList() ?? new List<int>();
                if (productIds.Any())
                {
                    _logger.LogInformation("Products to validate: {ProductIds}", string.Join(", ", productIds));
                    
                    var productMappings = await MapFakeStoreProductIdsToLocalAsync(productIds);
                    var invalidIds = productIds.Where(id => !productMappings.ContainsKey(id)).ToList();
                    
                    if (invalidIds.Any())
                    {
                        _logger.LogWarning("Products not found in local DB: {InvalidIds}", string.Join(", ", invalidIds));
                        result.Success = false;
                        result.Message = $"The following products do not exist in the local DB: {string.Join(", ", invalidIds)}";
                        result.InvalidProductIds = invalidIds;
                        return result;
                    }

                    _logger.LogInformation("All products exist in local DB");

                    // 4. Create local cart
                    _logger.LogInformation("Step 4: Creating local cart");
                    var localCart = await CreateLocalCartFromFakeStore(fakeStoreCart, productMappings, createdBy);
                    
                    // 5. Create external mapping
                    _logger.LogInformation("Step 5: Creating external mapping");
                    var snapshot = JsonSerializer.Serialize(fakeStoreCart);
                    await _cartRepository.CreateCartMappingAsync(fakeStoreCartId.ToString(), localCart.Id, ExternalSource.FakeStore, snapshot);

                    result.Success = true;
                    result.Message = "Cart synced successfully";
                    result.LocalCartId = localCart.Id;
                    result.ProductsSynced = productIds.Count;

                    _logger.LogInformation("=== SYNC SUCCESSFUL ===");
                    _logger.LogInformation("Cart {FakeStoreCartId} synced successfully as {LocalCartId}", 
                        fakeStoreCartId, localCart.Id);
                }
                else
                {
                    _logger.LogWarning("Cart {CartId} is empty", fakeStoreCartId);
                    result.Success = false;
                    result.Message = "Empty cart, cannot sync";
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "=== SYNC ERROR ===");
                _logger.LogError(ex, "Error syncing cart {CartId} from FakeStore. Details: {Message}", fakeStoreCartId, ex.Message);
                _logger.LogError("Stack trace: {StackTrace}", ex.StackTrace);
                
                result.Success = false;
                result.Message = $"Internal error: {ex.Message}";
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
                _logger.LogInformation("Starting bulk cart sync from FakeStore API");

                // 1. Get all carts from FakeStore
                var fakeStoreCarts = await _fakeStoreApiService.GetCartsAsync();
                var cartsList = fakeStoreCarts.ToList();

                batchResult.TotalCartsProcessed = cartsList.Count;

                // 2. Sync each cart
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
                batchResult.Message = $"Sync completed: {batchResult.CartsSuccessful} successful, {batchResult.CartsFailed} failed";

                _logger.LogInformation("Bulk sync completed: {Successful}/{Total} carts synced", 
                    batchResult.CartsSuccessful, batchResult.TotalCartsProcessed);

                return batchResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in bulk cart sync from FakeStore");
                batchResult.Success = false;
                batchResult.Message = $"Internal error in bulk sync: {ex.Message}";
                return batchResult;
            }
        }

        public async Task<CartDto?> ImportCartFromFakeStoreAsync(int fakeStoreCartId, Guid targetUserId, Guid createdBy)
        {
            try
            {
                _logger.LogInformation("Importing cart {CartId} from FakeStore for user {UserId}", fakeStoreCartId, targetUserId);

                // 1. Get cart from FakeStore
                var fakeStoreCart = await _fakeStoreApiService.GetCartByIdAsync(fakeStoreCartId);
                if (fakeStoreCart == null)
                {
                    _logger.LogWarning("Cart {CartId} not found in FakeStore", fakeStoreCartId);
                    return null;
                }

                // 2. Validate products
                var productIds = fakeStoreCart.Products?.Select(p => p.ProductId).ToList() ?? new List<int>();
                var productMappings = await MapFakeStoreProductIdsToLocalAsync(productIds);
                
                if (productIds.Count != productMappings.Count)
                {
                    throw new InvalidOperationException("Some products do not exist in the local database");
                }

                // 3. Create cart for specific user (do not create external mapping)
                var localCart = await CreateLocalCartFromFakeStore(fakeStoreCart, productMappings, createdBy, targetUserId);

                _logger.LogInformation("Cart {FakeStoreCartId} imported successfully as {LocalCartId} for user {UserId}", 
                    fakeStoreCartId, localCart.Id, targetUserId);

                return localCart.ToCartDto();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing cart {CartId} from FakeStore", fakeStoreCartId);
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
                    _logger.LogInformation("No products to map");
                    return new Dictionary<int, Guid>();
                }

                _logger.LogInformation("Mapping {Count} FakeStore product IDs to local IDs", 
                    fakeStoreProductIds.Count());
                _logger.LogDebug("IDs to map: {ProductIds}", string.Join(", ", fakeStoreProductIds));

                var sourceIds = fakeStoreProductIds.Select(id => id.ToString()).ToList();
                
                _logger.LogDebug("Querying mappings for sourceIds: {SourceIds}", string.Join(", ", sourceIds));
                var mappings = await _externalMappingRepository.GetInternalIdMappingsAsync(
                    sourceIds, ExternalSource.FakeStore, "PRODUCT");

                _logger.LogInformation("Mappings found: {MappingCount}", mappings.Count);
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
                        _logger.LogDebug("Added to result: {FakeStoreId} -> {LocalId}", fakeStoreId, mapping.Value);
                    }
                    else
                    {
                        _logger.LogWarning("Could not parse SourceId: {SourceId}", mapping.Key);
                    }
                }

                _logger.LogInformation("Mapped {MappedCount} of {RequestedCount} products", 
                    result.Count, fakeStoreProductIds.Count());

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error mapping product IDs. Details: {Message}", ex.Message);
                _logger.LogError("Stack trace: {StackTrace}", ex.StackTrace);
                throw new InvalidOperationException("Internal error mapping products.", ex);
            }
        }

        private async Task<Cart> CreateLocalCartFromFakeStore(
            FakeStoreCartResponse fakeStoreCart, 
            Dictionary<int, Guid> productMappings, 
            Guid createdBy, 
            Guid? specificUserId = null)
        {
            // Use system user by default (created in Program.cs)
            var systemUserId = new Guid("00000000-0000-0000-0000-000000000001");
            var finalUserId = specificUserId ?? systemUserId;
            
            _logger.LogInformation("Creating local cart for user: {UserId} (original FakeStore UserId: {FakeStoreUserId})", 
                finalUserId, fakeStoreCart.UserId);

            // Create local cart
            var localCart = new Cart
            {
                UserId = finalUserId,
                Status = CartStatus.Active,
                CreatedAt = fakeStoreCart.Date,
                UpdatedAt = DateTime.UtcNow
            };

            // Add cart items
            foreach (var fakeStoreProduct in fakeStoreCart.Products ?? new List<FakeStoreCartProduct>())
            {
                if (productMappings.TryGetValue(fakeStoreProduct.ProductId, out var localProductId))
                {
                    var cartItem = new CartItem
                    {
                        CartId = localCart.Id,
                        ProductId = localProductId,
                        Quantity = fakeStoreProduct.Quantity,
                        UnitPriceSnapshot = 0, // TODO: get price from local product
                        CreatedAt = DateTime.UtcNow
                    };

                    localCart.CartItems.Add(cartItem);
                    _logger.LogDebug("Added item: Product {ProductId}, Quantity {Quantity}", 
                        localProductId, fakeStoreProduct.Quantity);
                }
                else
                {
                    _logger.LogWarning("FakeStore product {ProductId} not found in mappings", fakeStoreProduct.ProductId);
                }
            }

            // Calculate totals (simplified)
            localCart.TotalBeforeDiscount = localCart.CartItems.Sum(ci => ci.UnitPriceSnapshot * ci.Quantity);
            localCart.FinalTotal = localCart.TotalBeforeDiscount;

            _logger.LogInformation("Local cart created with {ItemCount} items, Total: {Total}", 
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