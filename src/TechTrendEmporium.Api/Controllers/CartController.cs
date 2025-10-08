using Data.Entities.Enums;
using Logica.Interfaces;
using Logica.Models;
using Logica.Models.Carts;
using Logica.Mappers;
using Microsoft.AspNetCore.Mvc;

namespace TechTrendEmporium.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CartController : BaseController
    {
        private readonly ICartService _cartService;
        private readonly ILogger<CartController> _logger;

        public CartController(
            ICartService cartService,
            ILogger<CartController> logger)
        {
            _cartService = cartService;
            _logger = logger;
        }

        // Local Cart Operations

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<CartDto>> GetCart(Guid id)
        {
            try
            {
                var cart = await _cartService.GetCartByIdAsync(id);

                if (cart == null)
                {
                    return NotFound($"Cart with ID {id} not found");
                }

                return Ok(cart);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cart {CartId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost]
        public async Task<ActionResult<CartDto>> CreateCart(CreateCartRequest request)
        {
            try
            {
                var cart = await _cartService.CreateCartAsync(request);

                return CreatedAtAction(nameof(GetCart), new { id = cart.Id }, cart);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating cart");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPut("{id:guid}")]
        public async Task<ActionResult<CartDto>> UpdateCart(Guid id, UpdateCartRequest request)
        {
            try
            {
                var cart = await _cartService.UpdateCartAsync(id, request);

                if (cart == null)
                {
                    return NotFound($"Cart with ID {id} not found");
                }

                return Ok(cart);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating cart {CartId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpDelete("{id:guid}")]
        public async Task<ActionResult> DeleteCart(Guid id)
        {
            try
            {
                var success = await _cartService.DeleteCartAsync(id);

                if (!success)
                {
                    return NotFound($"Cart with ID {id} not found");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting cart {CartId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPatch("{id:guid}/abandon")]
        public async Task<ActionResult> AbandonCart(Guid id)
        {
            try
            {
                var success = await _cartService.SoftDeleteCartAsync(id);

                if (!success)
                {
                    return NotFound($"Cart with ID {id} not found");
                }

                return Ok(new { message = "Cart marked as abandoned" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error abandoning cart {CartId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        // FakeStore Operations

        [HttpGet("fakestore")]
        public async Task<ActionResult<IEnumerable<CartDto>>> GetCartsFromFakeStore()
        {
            try
            {
                var carts = await _cartService.GetCartsFromFakeStoreAsync();
                return Ok(carts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting carts from FakeStore");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("fakestore/{id:int}")]
        public async Task<ActionResult<CartDto>> GetCartFromFakeStore(int id)
        {
            try
            {
                var cart = await _cartService.GetCartFromFakeStoreAsync(id);

                if (cart == null)
                {
                    return NotFound($"Cart with ID {id} not found in FakeStore");
                }

                return Ok(cart);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cart {CartId} from FakeStore", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("fakestore/user/{userId:int}")]
        public async Task<ActionResult<IEnumerable<CartDto>>> GetUserCartsFromFakeStore(int userId)
        {
            try
            {
                var carts = await _cartService.GetUserCartsFromFakeStoreAsync(userId);
                return Ok(carts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user {UserId} carts from FakeStore", userId);
                return StatusCode(500, "Internal server error");
            }
        }

        // Sync Operations

        [HttpPost("sync-from-fakestore")]
        public async Task<ActionResult<object>> SyncAllCartsFromFakeStore()
        {
            try
            {
                var result = await _cartService.SyncAllCartsFromFakeStoreAsync();

                return Ok(new
                {
                    Message = "Cart synchronization completed successfully",
                    CartsSuccessful = result.CartsSuccessful,
                    CartsFailed = result.CartsFailed,
                    TotalProcessed = result.TotalCartsProcessed,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing carts from FakeStore");
                return StatusCode(500, "Error during synchronization");
            }
        }

        [HttpPost("import-from-fakestore/{fakeStoreId:int}")]
        public async Task<ActionResult<CartDto>> ImportCartFromFakeStore(int fakeStoreId)
        {
            try
            {
                var cart = await _cartService.ImportCartFromFakeStoreAsync(fakeStoreId);

                if (cart == null)
                {
                    return NotFound($"Cart with ID {fakeStoreId} not found in FakeStore");
                }

                return Ok(cart);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing cart {CartId} from FakeStore", fakeStoreId);
                return StatusCode(500, "Error during import");
            }
        }

        // === ENDPOINTS PARA INFORMACIÓN COMPLETA DE LA BD ===

        [HttpGet("admin/full-details")]
        public async Task<ActionResult<IEnumerable<CartFullDetailsDto>>> GetAllCartsFullDetails()
        {
            try
            {
                var carts = await _cartService.GetAllCartsFullDetailsAsync();
                return Ok(carts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all carts full details");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("admin/full-details/{id:guid}")]
        public async Task<ActionResult<CartFullDetailsDto>> GetCartFullDetails(Guid id)
        {
            try
            {
                var cart = await _cartService.GetCartFullDetailsByIdAsync(id);

                if (cart == null)
                {
                    return NotFound($"Cart with ID {id} not found");
                }

                return Ok(cart);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cart full details {CartId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("admin/full-details/user/{userId:guid}")]
        public async Task<ActionResult<IEnumerable<CartFullDetailsDto>>> GetCartsByUserFullDetails(Guid userId)
        {
            try
            {
                var carts = await _cartService.GetCartsByUserFullDetailsAsync(userId);
                return Ok(carts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting carts full details for user {UserId}", userId);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("admin/dashboard-summary")]
        public async Task<ActionResult<CartsDashboardSummaryDto>> GetCartsDashboardSummary()
        {
            try
            {
                var summary = await _cartService.GetCartsDashboardSummaryAsync();
                return Ok(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting carts dashboard summary");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("admin/full-details/status/{status}")]
        public async Task<ActionResult<IEnumerable<CartFullDetailsDto>>> GetCartsByStatusFullDetails(CartStatus status)
        {
            try
            {
                var carts = await _cartService.GetCartsByStatusFullDetailsAsync(status);
                return Ok(carts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting carts by status {Status} full details", status);
                return StatusCode(500, "Internal server error");
            }
        }

        // Utility methods

        private static Guid ConvertIntToGuid(int id)
        {
            var bytes = new byte[16];
            var idBytes = BitConverter.GetBytes(id);
            Array.Copy(idBytes, 0, bytes, 0, 4);
            return new Guid(bytes);
        }

        private static int ConvertGuidToInt(Guid guid)
        {
            var bytes = guid.ToByteArray();
            return BitConverter.ToInt32(bytes, 0);
        }
    }
}