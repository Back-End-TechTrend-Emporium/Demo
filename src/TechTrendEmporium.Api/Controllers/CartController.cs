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
        // [Authorize] // <- descomenta si quieres exigir token
        [SwaggerOperation(
            Summary = "Sincronizar cart específico desde FakeStore a BD local",
            Description = "Sincroniza un cart específico desde FakeStore API hacia la base de datos local",
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
                    return BadRequest("El ID del cart debe ser mayor a 0");
                }

                // TODO: Obtener el ID del usuario actual del JWT
                var createdBy = new Guid("00000000-0000-0000-0000-000000000001"); // Usuario sistema por ahora

                _logger.LogInformation("Iniciando sincronización del cart {CartId} desde FakeStore", cartId);
                var result = await _cartService.SyncCartFromFakeStoreAsync(cartId, createdBy);

                if (result.Success)
                {
                    _logger.LogInformation("Cart {CartId} sincronizado exitosamente como {LocalCartId}", 
                        cartId, result.LocalCartId);
                    return Ok(result);
                }
                else
                {
                    _logger.LogWarning("Fallo en sincronización del cart {CartId}: {Message}", 
                        cartId, result.Message);
                    return BadRequest(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sincronizando cart {CartId} desde FakeStore", cartId);
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    $"Error interno sincronizando cart {cartId}");
            }
        }

        // POST api/cart/sync-all-from-fakestore
        [HttpPost("sync-all-from-fakestore")]
        // [Authorize]
        [SwaggerOperation(
            Summary = "Sincronizar todos los carts desde FakeStore a BD local",
            Description = "Sincroniza todos los carts disponibles desde FakeStore API hacia la base de datos local",
            Tags = new[] { "Cart - Sync" }
        )]
        [ProducesResponseType(typeof(CartSyncBatchResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SyncAllCartsFromFakeStore(CancellationToken ct = default)
        {
            try
            {
                // TODO: Obtener el ID del usuario actual del JWT
                var createdBy = new Guid("00000000-0000-0000-0000-000000000001"); // Usuario sistema por ahora

                _logger.LogInformation("Iniciando sincronización masiva de carts desde FakeStore");
                var result = await _cartService.SyncAllCartsFromFakeStoreAsync(createdBy);

                _logger.LogInformation("Sincronización masiva completada: {Successful}/{Total} carts", 
                    result.CartsSuccessful, result.TotalCartsProcessed);
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en sincronización masiva de carts desde FakeStore");
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    "Error interno en sincronización masiva");
            }
        }

        // POST api/cart/import-from-fakestore/{cartId}
        [HttpPost("import-from-fakestore/{cartId:int}")]
        // [Authorize]
        [SwaggerOperation(
            Summary = "Importar cart desde FakeStore para usuario actual",
            Description = "Importa un cart específico desde FakeStore API y lo asigna al usuario actual",
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
                    return BadRequest("El ID del cart debe ser mayor a 0");
                }

                // TODO: Obtener el ID del usuario actual del JWT
                var currentUserId = new Guid("00000000-0000-0000-0000-000000000001"); // Usuario sistema por ahora
                var createdBy = currentUserId;

                _logger.LogInformation("Importando cart {CartId} desde FakeStore para usuario {UserId}", 
                    cartId, currentUserId);
                
                var importedCart = await _cartService.ImportCartFromFakeStoreAsync(cartId, currentUserId, createdBy);

                if (importedCart == null)
                {
                    return NotFound($"Cart con ID {cartId} no encontrado en FakeStore o no se pudo importar");
                }

                _logger.LogInformation("Cart {CartId} importado exitosamente para usuario {UserId}", 
                    cartId, currentUserId);
                
                return CreatedAtAction("GetCart", new { cartId = importedCart.Id }, importedCart);
            }
            catch (InvalidOperationException invEx)
            {
                _logger.LogWarning(invEx, "No se pudo importar cart {CartId}: {Message}", cartId, invEx.Message);
                return BadRequest(invEx.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importando cart {CartId} desde FakeStore", cartId);
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    $"Error interno importando cart {cartId}");
            }
        }
    }
}