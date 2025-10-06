using External.FakeStore.Models;
using Logica.Interfaces;
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

        // GET api/cart/fakestore
        [HttpGet("fakestore")]
        // [Authorize] // <- descomenta si quieres exigir token
        [SwaggerOperation(
            Summary = "Obtener todos los carts de FakeStore API", 
            Description = "Sincroniza y obtiene todos los carts disponibles desde FakeStore API",
            Tags = new[] { "Cart - FakeStore" }
        )]
        [ProducesResponseType(typeof(IEnumerable<FakeStoreCartResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetCartsFromFakeStore(CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation("Iniciando sincronización de carts desde FakeStore API");
                var carts = await _cartService.GetCartsFromFakeStoreAsync();
                
                _logger.LogInformation("Sincronización completada. {Count} carts obtenidos", carts.Count());
                return Ok(carts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante la sincronización de carts desde FakeStore API");
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    "Error durante la sincronización de carts");
            }
        }

        // GET api/cart/fakestore/{cartId}
        [HttpGet("fakestore/{cartId:int}")]
        // [Authorize]
        [SwaggerOperation(
            Summary = "Obtener cart específico de FakeStore API",
            Description = "Sincroniza y obtiene un cart específico desde FakeStore API por su ID",
            Tags = new[] { "Cart - FakeStore" }
        )]
        [ProducesResponseType(typeof(FakeStoreCartResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetCartFromFakeStore(int cartId, CancellationToken ct = default)
        {
            try
            {
                if (cartId <= 0)
                {
                    return BadRequest("El ID del cart debe ser mayor a 0");
                }

                _logger.LogInformation("Sincronizando cart {CartId} desde FakeStore API", cartId);
                var cart = await _cartService.GetCartFromFakeStoreAsync(cartId);

                if (cart == null)
                {
                    _logger.LogInformation("Cart {CartId} no encontrado en FakeStore API", cartId);
                    return NotFound($"Cart con ID {cartId} no encontrado en FakeStore");
                }

                _logger.LogInformation("Cart {CartId} sincronizado exitosamente", cartId);
                return Ok(cart);
            }
            catch (ArgumentException argEx)
            {
                _logger.LogWarning(argEx, "Parámetro inválido para cart {CartId}", cartId);
                return BadRequest(argEx.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sincronizando cart {CartId} desde FakeStore API", cartId);
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    $"Error sincronizando cart {cartId}");
            }
        }

        // POST api/cart/fakestore
        [HttpPost("fakestore")]
        // [Authorize]
        [SwaggerOperation(
            Summary = "Crear nuevo cart en FakeStore API",
            Description = "Crea un nuevo cart en FakeStore API con validación de productos locales",
            Tags = new[] { "Cart - FakeStore" }
        )]
        [ProducesResponseType(typeof(FakeStoreCartResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateCartInFakeStore([FromBody] FakeStoreCartCreateRequest cartRequest, CancellationToken ct = default)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return ValidationProblem(ModelState);
                }

                _logger.LogInformation("Creando nuevo cart en FakeStore API para usuario {UserId}", cartRequest.UserId);
                var cart = await _cartService.CreateCartInFakeStoreAsync(cartRequest);

                if (cart == null)
                {
                    return BadRequest("Error al crear cart en FakeStore API");
                }

                _logger.LogInformation("Cart {CartId} creado exitosamente en FakeStore API", cart.Id);
                return CreatedAtAction(nameof(GetCartFromFakeStore), new { cartId = cart.Id }, cart);
            }
            catch (ArgumentException argEx)
            {
                _logger.LogWarning(argEx, "Parámetros inválidos para crear cart");
                return BadRequest(argEx.Message);
            }
            catch (InvalidOperationException invEx)
            {
                _logger.LogWarning(invEx, "Operación inválida al crear cart");
                return BadRequest(invEx.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creando cart en FakeStore API");
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    "Error interno al crear cart");
            }
        }

        // PUT api/cart/fakestore/{cartId}
        [HttpPut("fakestore/{cartId:int}")]
        // [Authorize]
        [SwaggerOperation(
            Summary = "Actualizar cart en FakeStore API",
            Description = "Actualiza un cart existente en FakeStore API con validación de productos locales",
            Tags = new[] { "Cart - FakeStore" }
        )]
        [ProducesResponseType(typeof(FakeStoreCartResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateCartInFakeStore(int cartId, [FromBody] FakeStoreCartCreateRequest cartRequest, CancellationToken ct = default)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return ValidationProblem(ModelState);
                }

                if (cartId <= 0)
                {
                    return BadRequest("El ID del cart debe ser mayor a 0");
                }

                // Convertir a UpdateRequest
                var updateRequest = new FakeStoreCartUpdateRequest
                {
                    Id = cartId,
                    UserId = cartRequest.UserId,
                    Products = cartRequest.Products
                };

                _logger.LogInformation("Actualizando cart {CartId} en FakeStore API", cartId);
                var cart = await _cartService.UpdateCartInFakeStoreAsync(cartId, updateRequest);

                if (cart == null)
                {
                    _logger.LogInformation("Cart {CartId} no encontrado para actualizar", cartId);
                    return NotFound($"Cart con ID {cartId} no encontrado en FakeStore");
                }

                _logger.LogInformation("Cart {CartId} actualizado exitosamente", cartId);
                return Ok(cart);
            }
            catch (ArgumentException argEx)
            {
                _logger.LogWarning(argEx, "Parámetros inválidos para actualizar cart {CartId}", cartId);
                return BadRequest(argEx.Message);
            }
            catch (InvalidOperationException invEx)
            {
                _logger.LogWarning(invEx, "Operación inválida al actualizar cart {CartId}", cartId);
                return BadRequest(invEx.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error actualizando cart {CartId} en FakeStore API", cartId);
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    $"Error interno al actualizar cart {cartId}");
            }
        }

        // DELETE api/cart/fakestore/{cartId}
        [HttpDelete("fakestore/{cartId:int}")]
        // [Authorize]
        [SwaggerOperation(
            Summary = "Eliminar cart de FakeStore API",
            Description = "Elimina un cart específico en FakeStore API",
            Tags = new[] { "Cart - FakeStore" }
        )]
        [ProducesResponseType(typeof(FakeStoreCartResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteCartFromFakeStore(int cartId, CancellationToken ct = default)
        {
            try
            {
                if (cartId <= 0)
                {
                    return BadRequest("El ID del cart debe ser mayor a 0");
                }

                _logger.LogInformation("Eliminando cart {CartId} de FakeStore API", cartId);
                var cart = await _cartService.DeleteCartInFakeStoreAsync(cartId);

                if (cart == null)
                {
                    _logger.LogInformation("Cart {CartId} no encontrado para eliminar", cartId);
                    return NotFound($"Cart con ID {cartId} no encontrado en FakeStore");
                }

                _logger.LogInformation("Cart {CartId} eliminado exitosamente", cartId);
                return Ok(cart);
            }
            catch (ArgumentException argEx)
            {
                _logger.LogWarning(argEx, "Parámetro inválido para eliminar cart {CartId}", cartId);
                return BadRequest(argEx.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error eliminando cart {CartId} de FakeStore API", cartId);
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    $"Error interno al eliminar cart {cartId}");
            }
        }

        // GET api/cart/fakestore/{cartId}/validate-products
        [HttpGet("fakestore/{cartId:int}/validate-products")]
        // [Authorize]
        [SwaggerOperation(
            Summary = "Validar productos de cart en base de datos local",
            Description = "Valida que todos los productos de un cart de FakeStore existen en la base de datos local",
            Tags = new[] { "Cart - Validation" }
        )]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ValidateCartProducts(int cartId, CancellationToken ct = default)
        {
            try
            {
                if (cartId <= 0)
                {
                    return BadRequest("El ID del cart debe ser mayor a 0");
                }

                _logger.LogInformation("Validando productos del cart {CartId}", cartId);
                
                // Primero obtener el cart
                var cart = await _cartService.GetCartFromFakeStoreAsync(cartId);
                if (cart == null)
                {
                    return NotFound($"Cart con ID {cartId} no encontrado en FakeStore");
                }

                // Validar productos
                var productIds = cart.Products?.Select(p => p.ProductId).ToList() ?? new List<int>();
                var isValid = await _cartService.ValidateProductsExistInLocalDbAsync(productIds);
                var mappings = await _cartService.MapFakeStoreProductIdsToLocalAsync(productIds);

                var result = new
                {
                    CartId = cartId,
                    IsValid = isValid,
                    TotalProducts = productIds.Count,
                    ValidProducts = mappings.Count,
                    InvalidProducts = productIds.Where(id => !mappings.ContainsKey(id)).ToList(),
                    ProductMappings = mappings.Select(m => new
                    {
                        FakeStoreId = m.Key,
                        LocalId = m.Value
                    }).ToList()
                };

                _logger.LogInformation("Validación completada para cart {CartId}. Válido: {IsValid}", cartId, isValid);
                return Ok(result);
            }
            catch (ArgumentException argEx)
            {
                _logger.LogWarning(argEx, "Parámetro inválido para validar cart {CartId}", cartId);
                return BadRequest(argEx.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validando productos del cart {CartId}", cartId);
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    $"Error interno al validar cart {cartId}");
            }
        }
    }
}