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
        private readonly IProductRepository _productRepository;
        private readonly ILogger<CartService> _logger;

        public CartService(
            IFakeStoreApiService fakeStoreApiService,
            IExternalMappingRepository externalMappingRepository,
            ICartRepository cartRepository,
            IProductRepository productRepository,
            ILogger<CartService> logger)
        {
            _fakeStoreApiService = fakeStoreApiService ?? throw new ArgumentNullException(nameof(fakeStoreApiService));
            _externalMappingRepository = externalMappingRepository ?? throw new ArgumentNullException(nameof(externalMappingRepository));
            _cartRepository = cartRepository ?? throw new ArgumentNullException(nameof(cartRepository));
            _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // Local cart operations

        public async Task<CartDto?> GetCartByIdAsync(Guid id)
        {
            try
            {
                var cart = await _cartRepository.GetCartByIdAsync(id);
                return cart?.ToCartDtoExtended();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cart {CartId}", id);
                throw;
            }
        }

        public async Task<CartDto> CreateCartAsync(CreateCartRequest request)
        {
            try
            {
                var cart = new Cart
                {
                    UserId = request.UserId,
                    Status = request.Status,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    AppliedCouponId = request.CouponId
                };

                // Add cart items
                foreach (var itemRequest in request.Items)
                {
                    var product = await _productRepository.GetByIdAsync(itemRequest.ProductId);
                    if (product == null)
                    {
                        throw new InvalidOperationException($"Product {itemRequest.ProductId} not found");
                    }

                    var cartItem = new CartItem
                    {
                        CartId = cart.Id,
                        ProductId = itemRequest.ProductId,
                        Quantity = itemRequest.Quantity,
                        UnitPriceSnapshot = product.Price,
                        TitleSnapshot = product.Title,
                        ImageUrlSnapshot = product.ImageUrl,
                        CategoryNameSnapshot = product.Category?.Name,
                        CreatedAt = DateTime.UtcNow
                    };

                    cart.CartItems.Add(cartItem);
                }

                // Calculate totals
                cart.TotalBeforeDiscount = cart.CartItems.Sum(ci => ci.UnitPriceSnapshot * ci.Quantity);
                cart.FinalTotal = cart.TotalBeforeDiscount - cart.DiscountAmount;

                var createdCart = await _cartRepository.CreateCartAsync(cart);
                return createdCart.ToCartDtoExtended();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating cart");
                throw;
            }
        }

        public async Task<CartDto?> UpdateCartAsync(Guid id, UpdateCartRequest request)
        {
            try
            {
                var cart = await _cartRepository.GetCartByIdAsync(id);
                if (cart == null) return null;

                // Update cart properties
                if (request.Status.HasValue)
                {
                    cart.Status = request.Status.Value;
                }

                cart.AppliedCouponId = request.CouponId;

                // Update cart items ONLY if provided and not empty
                if (request.Items?.Any() == true)
                {
                    // Clear existing items
                    cart.CartItems.Clear();

                    // Add new items
                    foreach (var itemRequest in request.Items)
                    {
                        var product = await _productRepository.GetByIdAsync(itemRequest.ProductId);
                        if (product == null)
                        {
                            throw new InvalidOperationException($"Product {itemRequest.ProductId} not found");
                        }

                        var cartItem = new CartItem
                        {
                            CartId = cart.Id,
                            ProductId = itemRequest.ProductId,
                            Quantity = itemRequest.Quantity,
                            UnitPriceSnapshot = product.Price,
                            TitleSnapshot = product.Title,
                            ImageUrlSnapshot = product.ImageUrl,
                            CategoryNameSnapshot = product.Category?.Name,
                            CreatedAt = DateTime.UtcNow
                        };

                        cart.CartItems.Add(cartItem);
                    }

                    // Recalculate totals
                    cart.TotalBeforeDiscount = cart.CartItems.Sum(ci => ci.UnitPriceSnapshot * ci.Quantity);
                    cart.FinalTotal = cart.TotalBeforeDiscount - cart.DiscountAmount;
                }

                var updatedCart = await _cartRepository.UpdateCartAsync(cart);
                return updatedCart.ToCartDtoExtended();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating cart {CartId}", id);
                throw;
            }
        }

        public async Task<bool> DeleteCartAsync(Guid id)
        {
            try
            {
                return await _cartRepository.DeleteCartAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting cart {CartId}", id);
                throw;
            }
        }

        public async Task<bool> SoftDeleteCartAsync(Guid id)
        {
            try
            {
                return await _cartRepository.SoftDeleteCartAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error soft deleting cart {CartId}", id);
                throw;
            }
        }

        // FakeStore operations

        public async Task<IEnumerable<CartDto>> GetCartsFromFakeStoreAsync()
        {
            try
            {
                var fakeStoreCarts = await _fakeStoreApiService.GetCartsAsync();
                var cartDtos = new List<CartDto>();

                foreach (var fakeStoreCart in fakeStoreCarts)
                {
                    var cartDto = MapFakeStoreCartToDto(fakeStoreCart);
                    cartDtos.Add(cartDto);
                }

                return cartDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting carts from FakeStore");
                throw;
            }
        }

        public async Task<CartDto?> GetCartFromFakeStoreAsync(int id)
        {
            try
            {
                var fakeStoreCart = await _fakeStoreApiService.GetCartByIdAsync(id);
                return fakeStoreCart != null ? MapFakeStoreCartToDto(fakeStoreCart) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cart {CartId} from FakeStore", id);
                throw;
            }
        }

        public async Task<IEnumerable<CartDto>> GetUserCartsFromFakeStoreAsync(int userId)
        {
            try
            {
                var fakeStoreCarts = await _fakeStoreApiService.GetUserCartsAsync(userId);
                var cartDtos = new List<CartDto>();

                foreach (var fakeStoreCart in fakeStoreCarts)
                {
                    var cartDto = MapFakeStoreCartToDto(fakeStoreCart);
                    cartDtos.Add(cartDto);
                }

                return cartDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user {UserId} carts from FakeStore", userId);
                throw;
            }
        }

        // Sync operations

        public async Task<CartSyncResultDto> SyncCartFromFakeStoreAsync(int fakeStoreCartId, Guid createdBy)
        {
            var result = new CartSyncResultDto
            {
                FakeStoreCartId = fakeStoreCartId
            };

            try
            {
                _logger.LogInformation("=== CART SYNC START ===");
                _logger.LogInformation("Starting cart {CartId} sync from FakeStore", fakeStoreCartId);

                // 1. Check if already exists in local DB
                _logger.LogInformation("Step 1: Checking if cart already exists in local DB");
                var existingCart = await _cartRepository.GetCartByExternalIdAsync(fakeStoreCartId.ToString(), ExternalSource.FakeStore);
                if (existingCart != null)
                {
                    _logger.LogInformation("Cart {CartId} already exists in local DB with ID {LocalCartId}", fakeStoreCartId, existingCart.Id);
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
                
                if (!productIds.Any())
                {
                    _logger.LogWarning("Cart {CartId} is empty - no products to sync", fakeStoreCartId);
                    result.Success = false;
                    result.Message = "Empty cart, cannot sync";
                    return result;
                }

                _logger.LogInformation("Products to validate: {ProductIds}", string.Join(", ", productIds));
                
                var productMappings = await MapFakeStoreProductIdsToLocalAsync(productIds);
                var invalidIds = productIds.Where(id => !productMappings.ContainsKey(id)).ToList();
                
                if (invalidIds.Any())
                {
                    _logger.LogWarning("Products not found in local DB: {InvalidIds}", string.Join(", ", invalidIds));
                    _logger.LogWarning("Available mappings: {AvailableMappings}", string.Join(", ", productMappings.Keys));
                    result.Success = false;
                    result.Message = $"The following FakeStore products do not exist in the local DB: {string.Join(", ", invalidIds)}. " +
                                   $"Please sync products first using: POST /api/products/sync-from-fakestore";
                    result.InvalidProductIds = invalidIds;
                    return result;
                }

                _logger.LogInformation("All products exist in local DB. Creating local cart...");

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

                _logger.LogInformation("=== CART SYNC SUCCESSFUL ===");
                _logger.LogInformation("Cart {FakeStoreCartId} synced successfully as {LocalCartId}", 
                    fakeStoreCartId, localCart.Id);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "=== CART SYNC ERROR ===");
                _logger.LogError(ex, "Error syncing cart {CartId} from FakeStore. Details: {Message}", fakeStoreCartId, ex.Message);
                
                result.Success = false;
                result.Message = $"Internal error: {ex.Message}";
                result.Errors.Add(ex.Message);
                
                return result;
            }
        }

        public async Task<CartSyncBatchResultDto> SyncAllCartsFromFakeStoreAsync(Guid createdBy = default)
        {
            var batchResult = new CartSyncBatchResultDto();
            var systemUserId = new Guid("00000000-0000-0000-0000-000000000001");
            var finalCreatedBy = createdBy == default ? systemUserId : createdBy;

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
                    var syncResult = await SyncCartFromFakeStoreAsync(fakeStoreCart.Id, finalCreatedBy);
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

        public async Task<CartDto?> ImportCartFromFakeStoreAsync(int fakeStoreCartId, Guid targetUserId = default, Guid createdBy = default)
        {
            var systemUserId = new Guid("00000000-0000-0000-0000-000000000001");
            var finalTargetUserId = targetUserId == default ? systemUserId : targetUserId;
            var finalCreatedBy = createdBy == default ? systemUserId : createdBy;

            try
            {
                _logger.LogInformation("Importing cart {CartId} from FakeStore for user {UserId}", fakeStoreCartId, finalTargetUserId);

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
                var localCart = await CreateLocalCartFromFakeStore(fakeStoreCart, productMappings, finalCreatedBy, finalTargetUserId);

                _logger.LogInformation("Cart {FakeStoreCartId} imported successfully as {LocalCartId} for user {UserId}", 
                    fakeStoreCartId, localCart.Id, finalTargetUserId);

                return localCart.ToCartDtoExtended();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing cart {CartId} from FakeStore", fakeStoreCartId);
                throw;
            }
        }

        // === MÉTODOS PARA INFORMACIÓN COMPLETA DE LA BD ===

        public async Task<CartFullDetailsDto?> GetCartFullDetailsByIdAsync(Guid cartId)
        {
            try
            {
                var cart = await _cartRepository.GetCartByIdAsync(cartId);
                return cart?.ToCartFullDetailsDto();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cart full details {CartId}", cartId);
                throw;
            }
        }

        public async Task<IEnumerable<CartFullDetailsDto>> GetAllCartsFullDetailsAsync()
        {
            try
            {
                var carts = await _cartRepository.GetAllCartsAsync();
                return carts.Select(c => c.ToCartFullDetailsDto());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all carts full details");
                throw;
            }
        }

        public async Task<IEnumerable<CartFullDetailsDto>> GetCartsByUserFullDetailsAsync(Guid userId)
        {
            try
            {
                var carts = await _cartRepository.GetCartsByUserIdAsync(userId);
                return carts.Select(c => c.ToCartFullDetailsDto());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting carts full details for user {UserId}", userId);
                throw;
            }
        }

        public async Task<CartsDashboardSummaryDto> GetCartsDashboardSummaryAsync()
        {
            try
            {
                var allCarts = await _cartRepository.GetAllCartsAsync();
                return allCarts.ToCartsDashboardSummary();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting carts dashboard summary");
                throw;
            }
        }

        public async Task<IEnumerable<CartFullDetailsDto>> GetCartsByStatusFullDetailsAsync(Data.Entities.Enums.CartStatus status)
        {
            try
            {
                var allCarts = await _cartRepository.GetAllCartsAsync();
                var filteredCarts = allCarts.Where(c => c.Status == status);
                return filteredCarts.Select(c => c.ToCartFullDetailsDto());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting carts by status {Status} full details", status);
                throw;
            }
        }

        // Helper methods

        private CartDto MapFakeStoreCartToDto(FakeStoreCartResponse fakeStoreCart)
        {
            return new CartDto
            {
                Id = ConvertIntToGuid(fakeStoreCart.Id),
                UserId = ConvertIntToGuid(fakeStoreCart.UserId).ToString(),
                ShoppingCart = fakeStoreCart.Products?.Select(p => p.ProductId).ToList() ?? new List<int>(),
                CouponApplied = null,
                TotalBeforeDiscount = 0, // FakeStore doesn't provide totals
                TotalAfterDiscount = 0,
                ShippingCost = 0,
                FinalTotal = 0
            };
        }

        private async Task<Dictionary<int, Guid>> MapFakeStoreProductIdsToLocalAsync(IEnumerable<int> fakeStoreProductIds)
        {
            try
            {
                if (!fakeStoreProductIds?.Any() == true)
                {
                    return new Dictionary<int, Guid>();
                }

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

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error mapping product IDs");
                throw new InvalidOperationException("Internal error mapping products.", ex);
            }
        }

        private async Task<Cart> CreateLocalCartFromFakeStore(
            FakeStoreCartResponse fakeStoreCart, 
            Dictionary<int, Guid> productMappings, 
            Guid createdBy, 
            Guid? specificUserId = null)
        {
            var systemUserId = new Guid("00000000-0000-0000-0000-000000000001");
            var finalUserId = specificUserId ?? systemUserId;
            
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
                    // Get product details for price
                    var product = await _productRepository.GetByIdAsync(localProductId);
                    var unitPrice = product?.Price ?? 0;

                    var cartItem = new CartItem
                    {
                        CartId = localCart.Id,
                        ProductId = localProductId,
                        Quantity = fakeStoreProduct.Quantity,
                        UnitPriceSnapshot = unitPrice,
                        TitleSnapshot = product?.Title,
                        ImageUrlSnapshot = product?.ImageUrl,
                        CategoryNameSnapshot = product?.Category?.Name,
                        CreatedAt = DateTime.UtcNow
                    };

                    localCart.CartItems.Add(cartItem);
                }
            }

            // Calculate totals
            localCart.TotalBeforeDiscount = localCart.CartItems.Sum(ci => ci.UnitPriceSnapshot * ci.Quantity);
            localCart.FinalTotal = localCart.TotalBeforeDiscount;

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