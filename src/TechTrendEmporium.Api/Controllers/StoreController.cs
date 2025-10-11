using Logica.Models;
using Logica.Models.Products;
using Logica.Models.Category.Responses;
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
        
        public StoreController(IStoreService store, ICategoryService categoryService) 
        {
            _store = store;
            _categoryService = categoryService;
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
    }
}
