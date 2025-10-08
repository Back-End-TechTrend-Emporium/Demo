using Logica.Interfaces;
using Logica.Models;
using Logica.Models.Carts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace TechTrendEmporium.Api.Controllers
{
    [ApiController]
    [Route("api/cart")]
    public class CartController : ControllerBase
    {
        private readonly ICartService _cartService;
        private readonly ILogger<CartController> _logger;

        public CartController(ICartService cartService, ILogger<CartController> logger)
        {
            _cartService = cartService;
            _logger = logger;
        }

        // POST api/cart/sync-from-fakestore/{cartId}
        [HttpPost("sync-from-fakestore/{cartId:int}")]
        // [Authorize] // <- uncomment if you want to require token
        [SwaggerOperation(
            Summary = "Sync specific cart from FakeStore to local DB",
            Description = "Synchronizes a specific cart from FakeStore API to the local database",
            Tags = new[] { "Cart - Sync" }
        )]
        [ProducesResponseType(typeof(CartSyncResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SyncCartFromFakeStore(int cartId, CancellationToken ct = default)
        {
            try
            {
                if (cartId <= 0)
                {
                    return BadRequest("Cart ID must be greater than 0");
                }

                // TODO: Get current user ID from JWT
                var createdBy = new Guid("00000000-0000-0000-0000-000000000001"); // System user for now

                _logger.LogInformation("Starting cart {CartId} sync from FakeStore", cartId);
                var result = await _cartService.SyncCartFromFakeStoreAsync(cartId, createdBy);

                if (result.Success)
                {
                    _logger.LogInformation("Cart {CartId} synced successfully as {LocalCartId}", 
                        cartId, result.LocalCartId);
                    return Ok(result);
                }
                else
                {
                    _logger.LogWarning("Cart {CartId} sync failed: {Message}", 
                        cartId, result.Message);
                    return BadRequest(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing cart {CartId} from FakeStore", cartId);
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    $"Internal error syncing cart {cartId}");
            }
        }

        // POST api/cart/sync-all-from-fakestore
        [HttpPost("sync-all-from-fakestore")]
        // [Authorize]
        [SwaggerOperation(
            Summary = "Sync all carts from FakeStore to local DB",
            Description = "Synchronizes all available carts from FakeStore API to the local database",
            Tags = new[] { "Cart - Sync" }
        )]
        [ProducesResponseType(typeof(CartSyncBatchResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SyncAllCartsFromFakeStore(CancellationToken ct = default)
        {
            try
            {
                // TODO: Get current user ID from JWT
                var createdBy = new Guid("00000000-0000-0000-0000-000000000001"); // System user for now

                _logger.LogInformation("Starting bulk cart sync from FakeStore");
                var result = await _cartService.SyncAllCartsFromFakeStoreAsync(createdBy);

                _logger.LogInformation("Bulk sync completed: {Successful}/{Total} carts", 
                    result.CartsSuccessful, result.TotalCartsProcessed);
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in bulk cart sync from FakeStore");
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    "Internal error in bulk synchronization");
            }
        }

        // POST api/cart/import-from-fakestore/{cartId}
        [HttpPost("import-from-fakestore/{cartId:int}")]
        // [Authorize]
        [SwaggerOperation(
            Summary = "Import cart from FakeStore for current user",
            Description = "Imports a specific cart from FakeStore API and assigns it to the current user",
            Tags = new[] { "Cart - Import" }
        )]
        [ProducesResponseType(typeof(CartDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ImportCartFromFakeStore(int cartId, CancellationToken ct = default)
        {
            try
            {
                if (cartId <= 0)
                {
                    return BadRequest("Cart ID must be greater than 0");
                }

                // TODO: Get current user ID from JWT
                var currentUserId = new Guid("00000000-0000-0000-0000-000000000001"); // System user for now
                var createdBy = currentUserId;

                _logger.LogInformation("Importing cart {CartId} from FakeStore for user {UserId}", 
                    cartId, currentUserId);
                
                var importedCart = await _cartService.ImportCartFromFakeStoreAsync(cartId, currentUserId, createdBy);

                if (importedCart == null)
                {
                    return NotFound($"Cart with ID {cartId} not found in FakeStore or could not be imported");
                }

                _logger.LogInformation("Cart {CartId} imported successfully for user {UserId}", 
                    cartId, currentUserId);
                
                return CreatedAtAction("GetCart", new { cartId = importedCart.Id }, importedCart);
            }
            catch (InvalidOperationException invEx)
            {
                _logger.LogWarning(invEx, "Could not import cart {CartId}: {Message}", cartId, invEx.Message);
                return BadRequest(invEx.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing cart {CartId} from FakeStore", cartId);
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    $"Internal error importing cart {cartId}");
            }
        }
    }
}