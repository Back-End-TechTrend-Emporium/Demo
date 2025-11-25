using Data.Entities.Enums;
using Logica.Interfaces;
using Logica.Models;
using Logica.Models.Carts;
using Logica.Mappers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace TechTrendEmporium.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Shopper")]
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

        // === OPERACIONES DEL USUARIO AUTENTICADO ===

        [HttpGet("my-carts")]
        public async Task<ActionResult<IEnumerable<CartDto>>> GetMyCarts()
        {
            try
            {
                var userId = GetCurrentUserId();
                _logger.LogInformation("Getting carts for user {UserId}", userId);
                
                var carts = await _cartService.GetCartsByUserIdAsync(userId);
                return Ok(carts);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized("Invalid user token");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user carts");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("active")]
        public async Task<ActionResult<CartDto>> GetMyActiveCart()
        {
            try
            {
                var userId = GetCurrentUserId();
                _logger.LogInformation("Getting active cart for user {UserId}", userId);
                
                var cart = await _cartService.GetActiveCartByUserIdAsync(userId);
                if (cart == null)
                {
                    // Crear carrito activo si no existe
                    _logger.LogInformation("Creating new active cart for user {UserId}", userId);
                    cart = await _cartService.CreateEmptyCartForUserAsync(userId);
                }

                return Ok(cart);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized("Invalid user token");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active cart");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("add-item")]
        public async Task<ActionResult<CartDto>> AddItemToMyCart([FromBody] AddItemToCartRequest request)
        {
            try
            {
                // Debug logging para ver qu� se est� recibiendo
                _logger.LogInformation("Received request: {Request}", System.Text.Json.JsonSerializer.Serialize(request));
                
                // Validar que el request no sea null
                if (request == null)
                {
                    _logger.LogWarning("Request is null");
                    return BadRequest("Request body is required");
                }

                // Validar ModelState
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("ModelState is invalid: {Errors}", 
                        string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                    return BadRequest(ModelState);
                }

                // Validar campos espec�ficos
                if (request.ProductId == Guid.Empty)
                {
                    return BadRequest("ProductId is required and must be a valid GUID");
                }

                if (request.Quantity <= 0)
                {
                    return BadRequest("Quantity must be greater than 0");
                }

                var userId = GetCurrentUserId();
                _logger.LogInformation("User {UserId} adding item {ProductId} quantity {Quantity}", 
                    userId, request.ProductId, request.Quantity);
                
                var cart = await _cartService.AddItemToUserCartAsync(userId, request);
                return Ok(cart);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized("Invalid user token");
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Business logic error: {Message}", ex.Message);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding item to cart");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPut("update-item")]
        public async Task<ActionResult<CartDto>> UpdateItemInMyCart(UpdateCartItemQuantityRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                _logger.LogInformation("User {UserId} updating item {ProductId} to quantity {Quantity}", 
                    userId, request.ProductId, request.Quantity);
                
                var cart = await _cartService.UpdateItemInUserCartAsync(userId, request);
                return Ok(cart);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized("Invalid user token");
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating cart item");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpDelete("remove-item/{productId:guid}")]
        public async Task<ActionResult<CartDto>> RemoveItemFromMyCart(Guid productId)
        {
            try
            {
                var userId = GetCurrentUserId();
                _logger.LogInformation("User {UserId} removing item {ProductId}", userId, productId);
                
                var cart = await _cartService.RemoveItemFromUserCartAsync(userId, productId);
                return Ok(cart);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized("Invalid user token");
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing item from cart");
                return StatusCode(500, "Internal server error");
            }
        }
        [HttpPost("checkout")]
        public async Task<ActionResult<CartDto>> CheckoutMyCart([FromBody] CheckoutRequest request)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);
                var userId = GetCurrentUserId();
                var cart = await _cartService.CheckoutUserCartAsync(userId, request);
                return Ok(cart);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized("Invalid user token");
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception)
            {
                return StatusCode(500, "Internal server error");
            }
        }
        [HttpPost("clear")]
        public async Task<ActionResult<CartDto>> ClearMyCart()
        {
            try
            {
                var userId = GetCurrentUserId();
                _logger.LogInformation("User {UserId} clearing cart", userId);
                
                var cart = await _cartService.ClearUserCartAsync(userId);
                return Ok(cart);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized("Invalid user token");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cart");
                return StatusCode(500, "Internal server error");
            }
        }



    }
}