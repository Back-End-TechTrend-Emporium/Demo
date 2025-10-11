using Logica.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Logica.Models.Products;
using Microsoft.AspNetCore.Authorization;

namespace TechTrendEmporium.Api.Controllers
{
    [ApiController]
    [Route("api/product")]
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

        /// <summary>
        /// F01: Create a product
        /// As a Superadmin, Employee - I want to create a product
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Employee, SuperAdmin")]
        [ProducesResponseType(typeof(ProductCreateResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateProduct([FromBody] ProductCreateDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return ValidationProblem(ModelState);
                }

                var createdBy = GetCurrentUserId();
                var userRole = GetCurrentUserRole();

                var product = await _productService.CreateProductAsync(request, createdBy);

                // Auto-approve if SuperAdmin, leave pending if Employee
                if (userRole.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase))
                {
                    await _productService.ApproveProductAsync(product.Id, createdBy);
                }

                var response = new ProductCreateResponseDto
                {
                    ProductId = product.Id,
                    Message = "Successful"
                };

                return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product");

                var errorResponse = new ProductCreateResponseDto
                {
                    ProductId = Guid.Empty,
                    Message = "Failure"
                };

                return StatusCode(500, errorResponse);
            }
        }

        /// <summary>
        /// F02: View all products
        /// As a Superadmin, Employee, Shopper - I want to view all products
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ProductDto[]), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetProducts()
        {
            try
            {
                var products = await _productService.GetApprovedProductsAsync();
                return Ok(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting products");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// F03: Update product
        /// As a Superadmin, employee - I want to update field of a specific product
        /// </summary>
        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Employee, SuperAdmin")]
        [ProducesResponseType(typeof(ProductResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateProduct(Guid id, [FromBody] ProductUpdateDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return ValidationProblem(ModelState);
                }

                var product = await _productService.UpdateProductAsync(id, request);

                if (product == null)
                {
                    return NotFound($"Product with ID {id} not found");
                }

                var response = new ProductResponseDto
                {
                    Message = "Updated successfully"
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get individual product details (for admin purposes)
        /// </summary>
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<ProductDto>> GetProduct(Guid id)
        {
            try
            {
                var product = await _productService.GetProductByIdAsync(id);

                if (product == null)
                {
                    return NotFound($"Product with ID {id} not found");
                }

                return Ok(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting product {ProductId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get user's products (Employee/SuperAdmin)
        /// </summary>
        [HttpGet("my-products")]
        [Authorize(Roles = "Employee, SuperAdmin")]
        public async Task<ActionResult<IEnumerable<ProductSummaryDto>>> GetMyProducts()
        {
            try
            {
                var userId = GetCurrentUserId();
                var products = await _productService.GetProductsByUserIdAsync(userId);
                return Ok(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user products");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get approved products (detailed view for admin)
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
                _logger.LogError(ex, "Error getting approved products");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Delete product (SuperAdmin/Employee)
        /// </summary>
        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Employee, SuperAdmin")]
        public async Task<ActionResult<ProductResponseDto>> DeleteProduct(Guid id)
        {
            try
            {
                var success = await _productService.DeleteProductAsync(id);

                if (!success)
                {
                    return NotFound($"Product with ID {id} not found");
                }

                var response = new ProductResponseDto
                {
                    Message = "Deleted successfully"
                };
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Operación inválida al eliminar producto {ProductId}", id);
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product {ProductId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Search products
        /// </summary>
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<ProductDto>>> SearchProducts([FromQuery] string searchTerm)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    return BadRequest("Search term is required");
                }

                var products = await _productService.SearchProductsAsync(searchTerm);
                return Ok(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching products");
                return StatusCode(500, "Internal server error");
            }
        }

        // === APPROVAL OPERATIONS ===

        /// <summary>
        /// Get products pending approval
        /// </summary>
        [HttpGet("pending-approval")]
        [Authorize(Roles = "Employee, SuperAdmin")]
        public async Task<ActionResult<IEnumerable<ProductDto>>> GetPendingApproval()
        {
            try
            {
                var products = await _productService.GetPendingApprovalAsync();
                return Ok(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting products pending approval");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Approve product
        /// </summary>
        [HttpPost("{id:guid}/approve")]
        [Authorize(Roles = "Employee, SuperAdmin")]
        public async Task<ActionResult> ApproveProduct(Guid id)
        {
            try
            {
                var approvedBy = GetCurrentUserId();
                var success = await _productService.ApproveProductAsync(id, approvedBy);

                if (!success)
                {
                    return NotFound($"Product with ID {id} not found");
                }

                return Ok(new { Message = "Product approved successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving product {ProductId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Reject product
        /// </summary>
        [HttpPost("{id:guid}/reject")]
        [Authorize(Roles = "Employee, SuperAdmin")]
        public async Task<ActionResult> RejectProduct(Guid id)
        {
            try
            {
                var success = await _productService.RejectProductAsync(id);

                if (!success)
                {
                    return NotFound($"Product with ID {id} not found");
                }

                return Ok(new { Message = "Product rejected successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting product {ProductId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        // === FAKESTORE OPERATIONS ===

        /// <summary>
        /// Get products from FakeStore
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
                _logger.LogError(ex, "Error getting products from FakeStore");
                return StatusCode(500, "Error getting products from FakeStore");
            }
        }

        /// <summary>
        /// Get product from FakeStore by ID
        /// </summary>
        [HttpGet("fakestore/{id:int}")]
        public async Task<ActionResult<ProductDto>> GetProductFromFakeStore(int id)
        {
            try
            {
                var product = await _productService.GetProductFromFakeStoreAsync(id);
                if (product == null)
                {
                    return NotFound($"Product with ID {id} not found in FakeStore");
                }
                return Ok(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting product {ProductId} from FakeStore", id);
                return StatusCode(500, "Error getting product from FakeStore");
            }
        }

        /// <summary>
        /// Get categories from FakeStore
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
                _logger.LogError(ex, "Error getting categories from FakeStore");
                return StatusCode(500, "Error getting categories from FakeStore");
            }
        }

        /// <summary>
        /// Get products by category from FakeStore
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
                _logger.LogError(ex, "Error getting products by category from FakeStore");
                return StatusCode(500, "Error getting products by category from FakeStore");
            }
        }

        // === INVENTORY OPERATIONS ===

        /// <summary>
        /// Get product stock information
        /// </summary>
        [HttpGet("{id:guid}/stock")]
        public async Task<ActionResult<object>> GetProductStock(Guid id)
        {
            try
            {
                var product = await _productService.GetProductByIdAsync(id);
                if (product == null)
                {
                    return NotFound($"Product with ID {id} not found");
                }

                var stockInfo = new
                {
                    productId = product.Id,
                    productTitle = product.Title,
                    productImage = product.Image,
                    totalStock = product.InventoryTotal,
                    availableStock = product.InventoryAvailable,
                    reservedStock = Math.Max(0, product.InventoryTotal - product.InventoryAvailable),
                    isInStock = product.IsInStock,
                    isLowStock = product.IsLowStock,
                    isOutOfStock = product.IsOutOfStock
                };

                return Ok(stockInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting stock for product {ProductId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Update product stock
        /// </summary>
        [HttpPut("{id:guid}/stock")]
        [Authorize(Roles = "Employee, SuperAdmin")]
        public async Task<ActionResult> UpdateProductStock(Guid id, [FromBody] object stockUpdate)
        {
            try
            {
                _logger.LogInformation("?? Stock update requested for product {ProductId}: {Update}",
                    id, stockUpdate);

                return Ok(new
                {
                    message = "Stock update request logged",
                    productId = id,
                    updateData = stockUpdate
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating stock for product {ProductId}", id);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}