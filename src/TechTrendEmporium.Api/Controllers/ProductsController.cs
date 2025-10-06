using Logica.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Logica.Models.Products;

namespace TechTrendEmporium.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : BaseController
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


        [HttpGet("products")]
        public async Task<ActionResult<IEnumerable<ProductSummaryDto>>> GetMyProducts()
        {
            try
            {
                var userId = GetCurrentUserId(); // Del JWT cuando esté implementado
                var products = await _productService.GetProductsByUserIdAsync(userId);
                return Ok(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener productos del usuario");
                return StatusCode(500, "Error interno del servidor");
            }
        }


        [HttpGet("Allproducts")]
        public async Task<ActionResult<IEnumerable<ProductSummaryDto>>> GetAllProducts()
        {
            try
            {
                var products = await _productService.GetAllProductsAsync();
                var summaryProducts = products.Select(p => new ProductSummaryDto
                {
                    Id = p.Id,
                    Title = p.Title,
                    Price = p.Price,
                    Category = p.Category
                });
                return Ok(summaryProducts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener todos los productos");
                return StatusCode(500, "Error interno del servidor");
            }
        }

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


        [HttpPost]
        public async Task<ActionResult<ProductCreateResponseDto>> CreateProduct(ProductCreateDto productDto)
        {
            try
            {
                var createdBy = GetCurrentUserId();

                var product = await _productService.CreateProductAsync(productDto, createdBy);

                var response = new ProductCreateResponseDto
                {
                    ProductId = product.Id,
                    Message = "Successful"
                };

                return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear producto");

                var errorResponse = new ProductCreateResponseDto
                {
                    ProductId = Guid.Empty,
                    Message = "Failure"
                };

                return StatusCode(500, errorResponse);
            }
        }


        [HttpPut("{id:guid}")]
        public async Task<ActionResult<ProductResponseDto>> UpdateProduct(Guid id, ProductUpdateDto productDto)
        {
            try
            {
                var product = await _productService.UpdateProductAsync(id, productDto);

                if (product == null)
                {
                    return NotFound($"Producto con ID {id} no encontrado");
                }

                var response = new ProductResponseDto
                {
                    Message = "Updated successfuly"
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar producto {ProductId}", id);
                return StatusCode(500, "Error interno del servidor");
            }
        }


        [HttpDelete("{id:guid}")]
        public async Task<ActionResult<ProductResponseDto>> DeleteProduct(Guid id)
        {
            try
            {
                var success = await _productService.DeleteProductAsync(id);

                if (!success)
                {
                    return NotFound($"Producto con ID {id} no encontrado");
                }

                var response = new ProductResponseDto
                {
                    Message = "Deleted successfuly "
                };
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar producto {ProductId}", id);
                return StatusCode(500, "Error interno del servidor");
            }
        }


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



        // uso de la fake store


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



        // Sync Operations


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



        // Approval Operations


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



        // Utilities


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