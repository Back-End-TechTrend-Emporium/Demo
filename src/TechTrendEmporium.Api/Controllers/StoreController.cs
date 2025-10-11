using Logica.Models;
using Logica.Models.Products;
using Logica.Models.Category.Responses;
using Logica.Models.Review.Requests;
using Logica.Models.Review.Responses;
using Logica.Services;
using Logica.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace TechTrendEmporium.Api.Controllers
{
    [ApiController]
    [Route("api/store")]
    public class StoreController : ControllerBase
    {
        private readonly IStoreService _store;
        private readonly ICategoryService _categoryService;
        private readonly IProductService _productService;
        private readonly IReviewService _reviewService;
        
        public StoreController(
            IStoreService store, 
            ICategoryService categoryService, 
            IProductService productService,
            IReviewService reviewService) 
        {
            _store = store;
            _categoryService = categoryService;
            _productService = productService;
            _reviewService = reviewService;
        }

        /// <summary>
        /// F01: Product Display Page con filtros, orden y paginación.
        /// </summary>
        [HttpGet("products")]
        [ProducesResponseType(typeof(PagedResult<ProductListItemDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetProducts(
            [FromQuery] string? title,
            [FromQuery] decimal? price,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 12,
            [FromQuery] ProductSortBy sortBy = ProductSortBy.Title,   // enum: Title|Price|Rating
            [FromQuery] SortDirection sortDir = SortDirection.Asc,    // enum: Asc|Desc
            CancellationToken ct = default)
        {
            var query = new ProductQuery
            {
                Title = title,
                Price = price,
                Page = page,
                PageSize = pageSize,
                SortBy = sortBy,
                SortDir = sortDir
            };

            var result = await _store.GetProductsAsync(query, ct);
            return Ok(result); // usa Ok(result.Items) si quieres un array "puro"
        }

        /// <summary>
        /// Get products filtered by category with specific CategoryFilterResponseDto format.
        /// URL: /api/store/products/category?category={selected_category}
        /// </summary>
        [HttpGet("products/category")]
        [ProducesResponseType(typeof(CategoryFilterResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetProductsByCategory(
            [FromQuery] string category,
            [FromQuery] string? title,
            [FromQuery] decimal? price,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 12,
            [FromQuery] ProductSortBy sortBy = ProductSortBy.Title,
            [FromQuery] SortDirection sortDir = SortDirection.Asc,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(category))
            {
                return BadRequest("Category parameter is required");
            }

            // 1. Primero buscar la categoría por slug o nombre usando CategoryService
            var categoryEntity = await _categoryService.GetCategoryBySlugAsync(category);
            if (categoryEntity == null)
            {
                // Si no se encuentra por slug, buscar por nombre
                var allCategories = await _categoryService.GetApprovedCategoriesAsync();
                categoryEntity = allCategories.FirstOrDefault(c => 
                    c.Name.Equals(category, StringComparison.OrdinalIgnoreCase));
            }

            // 2. Obtener productos filtrados
            var query = new ProductQuery
            {
                Title = title,
                Price = price,
                Category = category,
                Page = page,
                PageSize = pageSize,
                SortBy = sortBy,
                SortDir = sortDir
            };

            var result = await _store.GetProductsAsync(query, ct);

            // 3. Usar el nombre real de la categoría encontrada, o el parámetro original si no se encuentra
            var selectedCategoryName = categoryEntity?.Name ?? category;

            var categoryResponse = new CategoryFilterResponseDto
            {
                SelectedCategory = selectedCategoryName,
                FilteredProducts = result.Items
            };

            return Ok(categoryResponse);
        }

        /// <summary>
        /// Get individual product details for store display
        /// URL: /api/store/products/{product_id}
        /// </summary>
        [HttpGet("products/{id:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetStoreProduct(Guid id)
        {
            try
            {
                var product = await _productService.GetProductByIdAsync(id);

                if (product == null)
                {
                    return NotFound($"Product with ID {id} not found");
                }

                // Crear la respuesta con el formato específico del store
                var response = new
                {
                    id = product.Id,
                    title = product.Title,
                    price = product.Price,
                    description = product.Description,
                    category = product.Category,
                    image = product.Image,
                    rating = product.Rating,
                    inventory = new
                    {
                        total = product.InventoryTotal,
                        available = product.InventoryAvailable
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                // You might want to inject ILogger here for proper logging
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get reviews for a specific product
        /// URL: /api/store/products/{productId}/reviews
        /// </summary>
        [HttpGet("products/{productId:guid}/reviews")]
        [ProducesResponseType(typeof(ReviewsResponseDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetProductReviews(Guid productId, CancellationToken ct = default)
        {
            var reviews = await _reviewService.GetByProductAsync(productId, ct);
            return Ok(reviews);
        }

        /// <summary>
        /// Add a review for a specific product
        /// URL: /api/store/products/{productId}/reviews/add
        /// </summary>
        [HttpPost("products/{productId:guid}/reviews/add")]
        [ProducesResponseType(typeof(ReviewDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AddProductReview(Guid productId, [FromBody] ReviewCreateDto body, CancellationToken ct = default)
        {
            if (!ModelState.IsValid) 
            {
                return ValidationProblem(ModelState);
            }

            try
            {
                var created = await _reviewService.AddAsync(productId, body, ct);
                return CreatedAtAction(nameof(GetProductReviews), new { productId }, created);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
