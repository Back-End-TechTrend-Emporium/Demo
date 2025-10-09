using Data.Entities;
using Data.Entities.Enums;
using External.FakeStore;
using External.FakeStore.Models;
using Logica.Interfaces;
using Logica.Mappers;
using Logica.Models;
using Logica.Models.Carts;
using Microsoft.EntityFrameworkCore;
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

        // === OPERACIONES CENTRADAS EN USUARIO (LÓGICA REAL) ===

        public async Task<IEnumerable<CartDto>> GetCartsByUserIdAsync(Guid userId)
        {
            try
            {
                var carts = await _cartRepository.GetCartsByUserIdAsync(userId);
                return carts.Select(c => c.ToCartDtoExtended());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting carts for user {UserId}", userId);
                throw;
            }
        }

        public async Task<CartDto?> GetActiveCartByUserIdAsync(Guid userId)
        {
            try
            {
                var cart = await _cartRepository.GetActiveCartByUserIdAsync(userId);
                return cart?.ToCartDtoExtended();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active cart for user {UserId}", userId);
                throw;
            }
        }

        public async Task<CartDto> CreateEmptyCartForUserAsync(Guid userId)
        {
            try
            {
                var cart = new Cart
                {
                    UserId = userId,
                    Status = CartStatus.Active,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    TotalBeforeDiscount = 0,
                    DiscountAmount = 0,
                    ShippingCost = 0,
                    FinalTotal = 0
                };

                var createdCart = await _cartRepository.CreateCartAsync(cart);
                return createdCart.ToCartDtoExtended();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating empty cart for user {UserId}", userId);
                throw;
            }
        }

        public async Task<CartDto> AddItemToUserCartAsync(Guid userId, AddItemToCartRequest request)
        {
            try
            {
                _logger.LogInformation("? User {UserId} adding item {ProductId} quantity {Quantity}", 
                    userId, request.ProductId, request.Quantity);

                // 1. Obtener o crear carrito activo
                var cart = await _cartRepository.GetActiveCartByUserIdAsync(userId);
                if (cart == null)
                {
                    _logger.LogInformation("Creating new cart for user {UserId}", userId);
                    cart = await CreateEmptyCartForUser(userId);
                }

                // 2. Verificar que el producto existe
                var product = await _productRepository.GetByIdAsync(request.ProductId);
                if (product == null)
                {
                    throw new InvalidOperationException($"Product {request.ProductId} not found");
                }

                // 3. Verificar si el item ya existe en el carrito
                var existingItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == request.ProductId);
                var currentQuantityInCart = existingItem?.Quantity ?? 0;
                var newTotalQuantity = currentQuantityInCart + request.Quantity;

                // ? VALIDAR DISPONIBILIDAD PARA RESERVAR
                if (product.InventoryAvailable < request.Quantity)
                {
                    throw new InvalidOperationException(
                        $"Not enough available stock for product '{product.Title}'. " +
                        $"Available: {product.InventoryAvailable}, Requested: {request.Quantity}");
                }

                // ? RESERVAR STOCK (Available ? Reserved)
                product.InventoryAvailable -= request.Quantity;
                // Nota: No sumamos a Reserved aquí porque Product no tiene ese campo
                // El Reserved se calcula como (Total - Available)
                product.UpdatedAt = DateTime.UtcNow;
                await _productRepository.UpdateAsync(product);

                _logger.LogInformation("?? Reserved {Quantity} units for product {ProductTitle}. " +
                    "Available: {PreviousAvailable} ? {NewAvailable}", 
                    request.Quantity, product.Title, 
                    product.InventoryAvailable + request.Quantity, product.InventoryAvailable);

                // 4. Agregar/actualizar item en carrito
                await _cartRepository.AddOrUpdateCartItemAsync(
                    cart.Id,
                    request.ProductId,
                    newTotalQuantity,
                    product.Price,
                    product.Title,
                    product.ImageUrl,
                    product.Category?.Name
                );

                _logger.LogInformation("?? Item added/updated. Total quantity in cart: {TotalQuantity}", newTotalQuantity);

                // 5. Recalcular y actualizar totales del carrito
                var updatedCart = await _cartRepository.GetCartByIdAsync(cart.Id);
                if (updatedCart != null)
                {
                    var totalBeforeDiscount = updatedCart.CartItems.Sum(ci => ci.UnitPriceSnapshot * ci.Quantity);
                    var finalTotal = totalBeforeDiscount - updatedCart.DiscountAmount + updatedCart.ShippingCost;
                    
                    await _cartRepository.UpdateCartTotalsAsync(
                        cart.Id,
                        totalBeforeDiscount,
                        updatedCart.DiscountAmount,
                        updatedCart.ShippingCost,
                        finalTotal
                    );
                }

                // 6. Obtener carrito final actualizado
                var finalCart = await _cartRepository.GetCartByIdAsync(cart.Id);
                
                _logger.LogInformation("? Item added successfully to cart {CartId} with stock reserved", cart.Id);
                
                return finalCart!.ToCartDtoExtended();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "?? Error adding item to cart for user {UserId}", userId);
                throw;
            }
        }

        public async Task<CartDto> UpdateItemInUserCartAsync(Guid userId, UpdateCartItemQuantityRequest request)
        {
            try
            {
                _logger.LogInformation("?? User {UserId} updating item {ProductId} to quantity {Quantity}", 
                    userId, request.ProductId, request.Quantity);

                // 1. Obtener carrito activo del usuario
                var cart = await _cartRepository.GetActiveCartByUserIdAsync(userId);
                if (cart == null)
                {
                    throw new InvalidOperationException("User has no active cart");
                }

                // 2. Verificar que el item existe en el carrito
                var existingItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == request.ProductId);
                if (existingItem == null)
                {
                    throw new InvalidOperationException("Product not found in cart");
                }

                // 3. Obtener producto para manejar stock
                var product = await _productRepository.GetByIdAsync(request.ProductId);
                if (product == null)
                {
                    throw new InvalidOperationException("Product not found");
                }

                var currentQuantityInCart = existingItem.Quantity;
                var quantityDifference = request.Quantity - currentQuantityInCart;

                // 4. Manejar cambios de reserva de stock
                if (quantityDifference > 0)
                {
                    // Aumentar cantidad - necesitamos reservar más stock
                    if (product.InventoryAvailable < quantityDifference)
                    {
                        throw new InvalidOperationException(
                            $"Not enough available stock for product '{product.Title}'. " +
                            $"Available: {product.InventoryAvailable}, Additional needed: {quantityDifference}");
                    }
                    
                    // Reservar stock adicional
                    product.InventoryAvailable -= quantityDifference;
                    _logger.LogInformation("?? Reserved additional {Quantity} units for product {ProductTitle}", 
                        quantityDifference, product.Title);
                }
                else if (quantityDifference < 0)
                {
                    // Reducir cantidad - devolver stock a disponible CON VALIDACIÓN
                    var quantityToRelease = Math.Abs(quantityDifference);
                    var newAvailableStock = product.InventoryAvailable + quantityToRelease;
                    
                    // ? VALIDAR CONSTRAINT: Available no puede superar Total
                    if (newAvailableStock > product.InventoryTotal)
                    {
                        var maxAvailableStock = product.InventoryTotal;
                        var actualStockToRelease = Math.Max(0, maxAvailableStock - product.InventoryAvailable);
                        
                        product.InventoryAvailable = maxAvailableStock;
                        
                        _logger.LogWarning("?? Stock release limited for product {ProductTitle}. " +
                            "Requested to release: {RequestedRelease}, Actual released: {ActualRelease}", 
                            product.Title, quantityToRelease, actualStockToRelease);
                    }
                    else
                    {
                        product.InventoryAvailable = newAvailableStock;
                        _logger.LogInformation("?? Released {Quantity} units back to available for product {ProductTitle}", 
                            quantityToRelease, product.Title);
                    }
                }

                // 5. Si quantity es 0 o negativa, eliminar el item
                if (request.Quantity <= 0)
                {
                    // Liberar todo el stock reservado CON VALIDACIÓN
                    var stockToRelease = currentQuantityInCart;
                    var newAvailableStock = product.InventoryAvailable + stockToRelease;
                    
                    // ? VALIDAR CONSTRAINT: Available no puede superar Total
                    if (newAvailableStock > product.InventoryTotal)
                    {
                        var maxAvailableStock = product.InventoryTotal;
                        product.InventoryAvailable = maxAvailableStock;
                        
                        _logger.LogWarning("?? Stock release limited when removing item for product {ProductTitle}. " +
                            "Available set to maximum: {MaxAvailable} (Total: {Total})", 
                            product.Title, maxAvailableStock, product.InventoryTotal);
                    }
                    else
                    {
                        product.InventoryAvailable = newAvailableStock;
                        _logger.LogInformation("?? Released {Quantity} units when removing item for product {ProductTitle}", 
                            stockToRelease, product.Title);
                    }
                    
                    product.UpdatedAt = DateTime.UtcNow;
                    await _productRepository.UpdateAsync(product);
                    
                    await _cartRepository.RemoveCartItemAsync(cart.Id, request.ProductId);
                    _logger.LogInformation("??? Item removed from cart and stock released safely");
                }
                else
                {
                    // Actualizar producto en BD
                    product.UpdatedAt = DateTime.UtcNow;
                    await _productRepository.UpdateAsync(product);

                    // Actualizar cantidad en carrito
                    await _cartRepository.AddOrUpdateCartItemAsync(
                        cart.Id,
                        request.ProductId,
                        request.Quantity,
                        existingItem.UnitPriceSnapshot,
                        existingItem.TitleSnapshot,
                        existingItem.ImageUrlSnapshot,
                        existingItem.CategoryNameSnapshot
                    );
                    _logger.LogInformation("?? Item quantity updated to {Quantity}", request.Quantity);
                }

                // 6. Recalcular y actualizar totales del carrito
                var updatedCart = await _cartRepository.GetCartByIdAsync(cart.Id);
                if (updatedCart != null)
                {
                    var totalBeforeDiscount = updatedCart.CartItems.Sum(ci => ci.UnitPriceSnapshot * ci.Quantity);
                    var finalTotal = totalBeforeDiscount - updatedCart.DiscountAmount + updatedCart.ShippingCost;
                    
                    await _cartRepository.UpdateCartTotalsAsync(
                        cart.Id,
                        totalBeforeDiscount,
                        updatedCart.DiscountAmount,
                        updatedCart.ShippingCost,
                        finalTotal
                    );
                }

                // 7. Obtener carrito final actualizado
                var finalCart = await _cartRepository.GetCartByIdAsync(cart.Id);
                
                _logger.LogInformation("? Item updated successfully in cart {CartId}", cart.Id);
                
                return finalCart!.ToCartDtoExtended();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "?? Error updating item in cart for user {UserId}", userId);
                throw;
            }
        }

        public async Task<CartDto> RemoveItemFromUserCartAsync(Guid userId, Guid productId)
        {
            try
            {
                _logger.LogInformation("??? User {UserId} removing item {ProductId}", userId, productId);

                // 1. Obtener carrito activo del usuario
                var cart = await _cartRepository.GetActiveCartByUserIdAsync(userId);
                if (cart == null)
                {
                    throw new InvalidOperationException("User has no active cart");
                }

                // 2. Verificar que el item existe en el carrito
                var existingItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == productId);
                if (existingItem == null)
                {
                    throw new InvalidOperationException("Product not found in cart");
                }

                // 3. Liberar stock reservado de vuelta a disponible CON VALIDACIÓN
                var product = await _productRepository.GetByIdAsync(productId);
                if (product != null)
                {
                    var stockToRelease = existingItem.Quantity;
                    var actualStockReleased = await ReleaseStockSafelyAsync(product, stockToRelease);

                    // Actualizar producto en BD (solo si hubo cambio)
                    if (actualStockReleased > 0)
                    {
                        product.UpdatedAt = DateTime.UtcNow;
                        await _productRepository.UpdateAsync(product);
                    }
                }

                // 4. Eliminar item del carrito
                await _cartRepository.RemoveCartItemAsync(cart.Id, productId);
                _logger.LogInformation("??? Item removed successfully from cart");

                // 5. Recalcular y actualizar totales del carrito
                var updatedCart = await _cartRepository.GetCartByIdAsync(cart.Id);
                if (updatedCart != null)
                {
                    var totalBeforeDiscount = updatedCart.CartItems.Sum(ci => ci.UnitPriceSnapshot * ci.Quantity);
                    var finalTotal = totalBeforeDiscount - updatedCart.DiscountAmount + updatedCart.ShippingCost;
                    
                    await _cartRepository.UpdateCartTotalsAsync(
                        cart.Id,
                        totalBeforeDiscount,
                        updatedCart.DiscountAmount,
                        updatedCart.ShippingCost,
                        finalTotal
                    );
                }

                // 6. Obtener carrito final actualizado
                var finalCart = await _cartRepository.GetCartByIdAsync(cart.Id);
                
                _logger.LogInformation("? Item removed successfully from cart {CartId} and stock released safely", cart.Id);
                
                return finalCart!.ToCartDtoExtended();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "?? Error removing item from cart for user {UserId}", userId);
                throw;
            }
        }

        public async Task<CartDto> ClearUserCartAsync(Guid userId)
        {
            try
            {
                var cart = await _cartRepository.GetActiveCartByUserIdAsync(userId);
                if (cart == null)
                {
                    throw new InvalidOperationException("User has no active cart");
                }

                cart.CartItems.Clear();
                RecalculateCartTotals(cart);

                var updatedCart = await _cartRepository.UpdateCartAsync(cart);
                return updatedCart.ToCartDtoExtended();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cart for user {UserId}", userId);
                throw;
            }
        }

        public async Task<CartDto> CheckoutUserCartAsync(Guid userId)
        {
            try
            {
                var cart = await _cartRepository.GetActiveCartByUserIdAsync(userId);
                if (cart == null)
                {
                    throw new InvalidOperationException("User has no active cart");
                }

                if (!cart.CartItems.Any())
                {
                    throw new InvalidOperationException("Cannot checkout empty cart");
                }

                _logger.LogInformation("?? Processing checkout for user {UserId}, cart {CartId} with {ItemCount} items", 
                    userId, cart.Id, cart.CartItems.Count);

                // === VALIDAR INVENTARIO EN CHECKOUT (Stock ya reservado) ===
                var validationErrors = new List<string>();

                // ? USAR TOLIST() PARA EVITAR "COLLECTION MODIFIED" ERROR
                var cartItemsList = cart.CartItems.ToList(); // ? FIX AQUÍ

                foreach (var cartItem in cartItemsList)
                {
                    var product = await _productRepository.GetByIdAsync(cartItem.ProductId);
                    if (product == null)
                    {
                        validationErrors.Add($"Product '{cartItem.TitleSnapshot}' no longer exists");
                        continue;
                    }

                    // ? VALIDAR CONTRA TOTAL STOCK (no Available, porque ya está reservado)
                    if (product.InventoryTotal < cartItem.Quantity)
                    {
                        validationErrors.Add(
                            $"Not enough total inventory for product '{product.Title}'. " +
                            $"Total Stock: {product.InventoryTotal}, " +
                            $"Requested: {cartItem.Quantity}");
                        continue;
                    }

                    _logger.LogInformation("? Checkout validation passed for {ProductTitle}: " +
                        "Total Stock {TotalStock} >= Requested {Requested} (Available: {Available}, Reserved: {Reserved})", 
                        product.Title, product.InventoryTotal, cartItem.Quantity, 
                        product.InventoryAvailable, product.InventoryTotal - product.InventoryAvailable);
                }

                // Si hay errores de validación, no procesar el checkout
                if (validationErrors.Any())
                {
                    var errorMessage = "Checkout failed due to inventory issues:\n" + string.Join("\n", validationErrors);
                    _logger.LogWarning("? Checkout validation failed: {Errors}", string.Join(", ", validationErrors));
                    throw new InvalidOperationException(errorMessage);
                }

                // === PROCESAR CHECKOUT: CONFIRMAR VENTA ===
                // ? USAR TOLIST() PARA EVITAR "COLLECTION MODIFIED" ERROR
                foreach (var cartItem in cartItemsList) // ? YA TENEMOS LA LISTA
                {
                    var product = await _productRepository.GetByIdAsync(cartItem.ProductId);
                    if (product != null)
                    {
                        // El stock ya fue reservado al agregar al carrito
                        // Ahora solo reducimos el total para confirmar la venta
                        var previousTotal = product.InventoryTotal;
                        product.InventoryTotal -= cartItem.Quantity;
                        
                        // Asegurar que no quede negativo
                        if (product.InventoryTotal < 0)
                        {
                            product.InventoryTotal = 0;
                        }
                        
                        product.UpdatedAt = DateTime.UtcNow;
                        
                        _logger.LogInformation("?? Sale confirmed for product {ProductTitle}: " +
                            "Total Stock {PreviousTotal} ? {NewTotal} (-{Quantity}). " +
                            "Available remains: {Available}", 
                            product.Title, previousTotal, product.InventoryTotal, cartItem.Quantity,
                            product.InventoryAvailable);
                        
                        await _productRepository.UpdateAsync(product);
                    }
                }

                // === MARCAR CARRITO COMO COMPRADO ===
                cart.Status = CartStatus.CheckedOut;
                cart.UpdatedAt = DateTime.UtcNow;

                var updatedCart = await _cartRepository.UpdateCartAsync(cart);
                
                _logger.LogInformation("? Checkout completed successfully for user {UserId}. Cart {CartId} checked out with {ItemCount} items", 
                    userId, cart.Id, cart.CartItems.Count);

                return updatedCart.ToCartDtoExtended();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "?? Error during checkout for user {UserId}", userId);
                throw;
            }
        }

        // === MÉTODOS HELPER ===

        private async Task<Cart> CreateEmptyCartForUser(Guid userId)
        {
            var cart = new Cart
            {
                UserId = userId,
                Status = CartStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                TotalBeforeDiscount = 0,
                DiscountAmount = 0,
                ShippingCost = 0,
                FinalTotal = 0
            };

            return await _cartRepository.CreateCartAsync(cart);
        }

        private void RecalculateCartTotals(Cart cart)
        {
            cart.TotalBeforeDiscount = cart.CartItems.Sum(ci => ci.UnitPriceSnapshot * ci.Quantity);
            cart.FinalTotal = cart.TotalBeforeDiscount - cart.DiscountAmount + cart.ShippingCost;
            cart.UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Verifica si hay suficiente inventario para un producto
        /// </summary>
        private async Task ValidateInventoryAsync(Guid productId, int requestedQuantity, int currentCartQuantity = 0)
        {
            var product = await _productRepository.GetByIdAsync(productId);
            if (product == null)
            {
                throw new InvalidOperationException($"Product {productId} not found");
            }

            var totalRequested = requestedQuantity + currentCartQuantity;
            if (product.InventoryAvailable < totalRequested)
            {
                throw new InvalidOperationException(
                    $"Not enough inventory for product '{product.Title}'. " +
                    $"Available: {product.InventoryAvailable}, Requested: {totalRequested}");
            }
        }

        /// <summary>
        /// Restaura inventario cuando se cancela o abandona un carrito
        /// </summary>
        public async Task RestoreInventoryAsync(Guid cartId)
        {
            try
            {
                var cart = await _cartRepository.GetCartByIdAsync(cartId);
                if (cart == null || cart.Status != CartStatus.CheckedOut)
                {
                    return; // Solo restaurar si el carrito fue comprado
                }

                _logger.LogInformation("?? Restoring inventory for cancelled cart {CartId}", cartId);

                foreach (var cartItem in cart.CartItems)
                {
                    var product = await _productRepository.GetByIdAsync(cartItem.ProductId);
                    if (product != null)
                    {
                        product.InventoryAvailable += cartItem.Quantity;
                        product.UpdatedAt = DateTime.UtcNow;
                        await _productRepository.UpdateAsync(product);
                        
                        _logger.LogInformation("?? Restored {Quantity} units for product {ProductTitle}", 
                            cartItem.Quantity, product.Title);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring inventory for cart {CartId}", cartId);
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

        /// <summary>
        /// Obtiene warnings de inventario para mostrar en frontend sin bloquear operaciones
        /// </summary>
        public async Task<IEnumerable<string>> GetInventoryWarningsAsync(Guid userId)
        {
            try
            {
                var cart = await _cartRepository.GetActiveCartByUserIdAsync(userId);
                if (cart == null || !cart.CartItems.Any())
                {
                    return new List<string>();
                }

                var warnings = new List<string>();

                foreach (var cartItem in cart.CartItems)
                {
                    var product = await _productRepository.GetByIdAsync(cartItem.ProductId);
                    if (product == null)
                    {
                        warnings.Add($"?? Product '{cartItem.TitleSnapshot}' is no longer available");
                        continue;
                    }

                    if (product.InventoryAvailable < cartItem.Quantity)
                    {
                        warnings.Add(
                            $"?? Limited stock for '{product.Title}': " +
                            $"Only {product.InventoryAvailable} available (you have {cartItem.Quantity} in cart)");
                    }
                    else if (product.InventoryAvailable < cartItem.Quantity * 2)
                    {
                        warnings.Add($"?? Low stock for '{product.Title}': Only {product.InventoryAvailable} remaining");
                    }
                }

                return warnings;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting inventory warnings for user {UserId}", userId);
                return new List<string> { "?? Unable to check inventory status" };
            }
        }

        /// <summary>
        /// Libera stock de forma segura respetando los constraints de BD
        /// </summary>
        private async Task<int> ReleaseStockSafelyAsync(Product product, int quantityToRelease)
        {
            var newAvailableStock = product.InventoryAvailable + quantityToRelease;
            
            // Validar constraint: Available no puede superar Total
            if (newAvailableStock > product.InventoryTotal)
            {
                var maxAvailableStock = product.InventoryTotal;
                var actualStockReleased = Math.Max(0, maxAvailableStock - product.InventoryAvailable);
                
                product.InventoryAvailable = maxAvailableStock;
                
                _logger.LogWarning("?? Stock release limited for product {ProductTitle}. " +
                    "Requested: {RequestedRelease}, Actual: {ActualRelease}, " +
                    "Available: {Available}, Total: {Total}", 
                    product.Title, quantityToRelease, actualStockReleased,
                    product.InventoryAvailable, product.InventoryTotal);
                
                return actualStockReleased;
            }
            else
            {
                product.InventoryAvailable = newAvailableStock;
                
                _logger.LogInformation("?? Released {Quantity} units for product {ProductTitle}. " +
                    "Available: {Available}, Total: {Total}", 
                    quantityToRelease, product.Title,
                    product.InventoryAvailable, product.InventoryTotal);
                
                return quantityToRelease;
            }
        }
    }
}