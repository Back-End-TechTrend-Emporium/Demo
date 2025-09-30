using Logica.Models;
using Logica.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace TechTrendEmporium.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(
            IProductService productService,
            ILogger<ProductsController> logger)
        {
            _productService = productService;
            _logger = logger;
        }

        #region CRUD Operations (Local Database)

        /// <summary>
        /// Obtiene todos los productos de la base de datos local
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProductDto>>> GetAllProducts()
        {
            try
            {
                var products = await _productService.GetAllProductsAsync();
                return Ok(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener todos los productos");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Obtiene solo los productos aprobados de la base de datos local
        /// </summary>
        [HttpGet("approved")]
        public async Task<ActionResult<IEnumerable<ProductDto>>> GetApprovedProducts()
        {
            try
            {
                var products = await _productService.GetApprovedProductsAsync();
                return Ok(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener productos aprobados");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Obtiene un producto específico por ID de la base de datos local
        /// </summary>
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<ProductDto>> GetProduct(Guid id)
        {
            try
            {
                var product = await _productService.GetProductByIdAsync(id);
                
                if (product == null)
                {
                    return NotFound($"Producto con ID {id} no encontrado");
                }
                
                return Ok(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener producto {ProductId}", id);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Crea un nuevo producto en la base de datos local
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ProductDto>> CreateProduct(ProductCreateDto productDto)
        {
            try
            {
                // En un sistema real, obtener el userId del token JWT
                var createdBy = GetCurrentUserId();
                
                var product = await _productService.CreateProductAsync(productDto, createdBy);
                
                return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear producto");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Actualiza un producto existente en la base de datos local
        /// </summary>
        [HttpPut("{id:guid}")]
        public async Task<ActionResult<ProductDto>> UpdateProduct(Guid id, ProductUpdateDto productDto)
        {
            try
            {
                var product = await _productService.UpdateProductAsync(id, productDto);
                
                if (product == null)
                {
                    return NotFound($"Producto con ID {id} no encontrado");
                }
                
                return Ok(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar producto {ProductId}", id);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Elimina un producto de la base de datos local (soft delete)
        /// </summary>
        [HttpDelete("{id:guid}")]
        public async Task<ActionResult> DeleteProduct(Guid id)
        {
            try
            {
                var success = await _productService.DeleteProductAsync(id);
                
                if (!success)
                {
                    return NotFound($"Producto con ID {id} no encontrado");
                }
                
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar producto {ProductId}", id);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Busca productos en la base de datos local
        /// </summary>
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<ProductDto>>> SearchProducts([FromQuery] string searchTerm)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    return BadRequest("El término de búsqueda es requerido");
                }

                var products = await _productService.SearchProductsAsync(searchTerm);
                return Ok(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en búsqueda de productos");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        #endregion

        #region FakeStore API Operations

        /// <summary>
        /// Obtiene productos directamente desde FakeStore API (sin guardar en BD)
        /// </summary>
        [HttpGet("fakestore")]
        public async Task<ActionResult<IEnumerable<ProductDto>>> GetProductsFromFakeStore()
        {
            try
            {
                var products = await _productService.GetProductsFromFakeStoreAsync();
                return Ok(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener productos de FakeStore");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Obtiene un producto específico desde FakeStore API
        /// </summary>
        [HttpGet("fakestore/{id:int}")]
        public async Task<ActionResult<ProductDto>> GetProductFromFakeStore(int id)
        {
            try
            {
                var product = await _productService.GetProductFromFakeStoreAsync(id);
                
                if (product == null)
                {
                    return NotFound($"Producto con ID {id} no encontrado en FakeStore");
                }
                
                return Ok(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener producto {ProductId} de FakeStore", id);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Obtiene categorías desde FakeStore API
        /// </summary>
        [HttpGet("fakestore/categories")]
        public async Task<ActionResult<IEnumerable<string>>> GetCategoriesFromFakeStore()
        {
            try
            {
                var categories = await _productService.GetCategoriesFromFakeStoreAsync();
                return Ok(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener categorías de FakeStore");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Obtiene productos por categoría desde FakeStore API
        /// </summary>
        [HttpGet("fakestore/category/{category}")]
        public async Task<ActionResult<IEnumerable<ProductDto>>> GetProductsByCategoryFromFakeStore(string category)
        {
            try
            {
                var products = await _productService.GetProductsByCategoryFromFakeStoreAsync(category);
                return Ok(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener productos de categoría {Category} de FakeStore", category);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        #endregion

        #region Sync Operations

        /// <summary>
        /// Sincroniza todos los productos desde FakeStore a la base de datos local
        /// </summary>
        [HttpPost("sync-from-fakestore")]
        public async Task<ActionResult<object>> SyncAllFromFakeStore()
        {
            try
            {
                var createdBy = GetCurrentUserId();
                var importedCount = await _productService.SyncAllFromFakeStoreAsync(createdBy);
                
                return Ok(new
                {
                    Message = "Sincronización completada exitosamente",
                    ImportedCount = importedCount,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en sincronización desde FakeStore");
                return StatusCode(500, "Error durante la sincronización");
            }
        }

        /// <summary>
        /// Importa un producto específico desde FakeStore a la base de datos local
        /// </summary>
        [HttpPost("import-from-fakestore/{fakeStoreId:int}")]
        public async Task<ActionResult<ProductDto>> ImportProductFromFakeStore(int fakeStoreId)
        {
            try
            {
                var createdBy = GetCurrentUserId();
                var product = await _productService.ImportProductFromFakeStoreAsync(fakeStoreId, createdBy);
                
                if (product == null)
                {
                    return NotFound($"Producto con ID {fakeStoreId} no encontrado en FakeStore");
                }
                
                return Ok(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importando producto {ProductId} desde FakeStore", fakeStoreId);
                return StatusCode(500, "Error durante la importación");
            }
        }

        #endregion

        #region Approval Operations

        /// <summary>
        /// Obtiene productos pendientes de aprobación
        /// </summary>
        [HttpGet("pending-approval")]
        public async Task<ActionResult<IEnumerable<ProductDto>>> GetPendingApproval()
        {
            try
            {
                var products = await _productService.GetPendingApprovalAsync();
                return Ok(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener productos pendientes de aprobación");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Aprueba un producto
        /// </summary>
        [HttpPost("{id:guid}/approve")]
        public async Task<ActionResult> ApproveProduct(Guid id)
        {
            try
            {
                var approvedBy = GetCurrentUserId();
                var success = await _productService.ApproveProductAsync(id, approvedBy);
                
                if (!success)
                {
                    return NotFound($"Producto con ID {id} no encontrado");
                }
                
                return Ok(new { Message = "Producto aprobado exitosamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al aprobar producto {ProductId}", id);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Rechaza un producto
        /// </summary>
        [HttpPost("{id:guid}/reject")]
        public async Task<ActionResult> RejectProduct(Guid id)
        {
            try
            {
                var success = await _productService.RejectProductAsync(id);
                
                if (!success)
                {
                    return NotFound($"Producto con ID {id} no encontrado");
                }
                
                return Ok(new { Message = "Producto rechazado exitosamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al rechazar producto {ProductId}", id);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Obtiene el ID del usuario actual desde el token JWT (temporal)
        /// </summary>
        private Guid GetCurrentUserId()
        {
            // En un sistema real, esto vendría del ClaimsPrincipal
            // Por ahora, devolver un GUID fijo para testing
            return Guid.Parse("00000000-0000-0000-0000-000000000001");
        }

        #endregion
    }
}