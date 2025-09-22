using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechTrendEmporium.Core.DTOs;
using TechTrendEmporium.Core.Entities;
using TechTrendEmporium.Core.Interfaces;

namespace TechTrendEmporium.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFakeStoreApiService _fakeStoreApi;

    public CategoriesController(IUnitOfWork unitOfWork, IFakeStoreApiService fakeStoreApi)
    {
        _unitOfWork = unitOfWork;
        _fakeStoreApi = fakeStoreApi;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CategoryDto>>> GetCategories()
    {
        var categories = await _unitOfWork.Categories.GetAllAsync();
        var categoryDtos = categories.Select(c => MapToCategoryDto(c));
        
        return Ok(categoryDtos);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CategoryDto>> GetCategory(int id)
    {
        var category = await _unitOfWork.Categories.GetByIdAsync(id);
        if (category == null)
        {
            return NotFound();
        }

        return Ok(MapToCategoryDto(category));
    }

    [HttpPost]
    [Authorize(Policy = "EmployeeOnly")]
    public async Task<ActionResult<CategoryDto>> CreateCategory([FromBody] CreateCategoryRequest request)
    {
        // Check if category with same name already exists
        var existingCategory = await _unitOfWork.Categories.GetByNameAsync(request.Name);
        if (existingCategory != null)
        {
            return BadRequest("Category with this name already exists");
        }

        var category = new Category
        {
            Name = request.Name,
            Description = request.Description,
            ImageUrl = request.ImageUrl,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Categories.AddAsync(category);
        await _unitOfWork.SaveChangesAsync();

        return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, MapToCategoryDto(category));
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "EmployeeOnly")]
    public async Task<ActionResult<CategoryDto>> UpdateCategory(int id, [FromBody] UpdateCategoryRequest request)
    {
        var category = await _unitOfWork.Categories.GetByIdAsync(id);
        if (category == null)
        {
            return NotFound();
        }

        // Check if another category with same name exists
        var existingCategory = await _unitOfWork.Categories.GetByNameAsync(request.Name);
        if (existingCategory != null && existingCategory.Id != id)
        {
            return BadRequest("Category with this name already exists");
        }

        category.Name = request.Name;
        category.Description = request.Description;
        category.ImageUrl = request.ImageUrl;
        category.IsActive = request.IsActive;
        category.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Categories.UpdateAsync(category);
        await _unitOfWork.SaveChangesAsync();

        return Ok(MapToCategoryDto(category));
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var category = await _unitOfWork.Categories.GetByIdAsync(id);
        if (category == null)
        {
            return NotFound();
        }

        // Check if category has products
        var products = await _unitOfWork.Products.GetByCategoryAsync(id);
        if (products.Any())
        {
            return BadRequest("Cannot delete category that has products. Move or delete products first.");
        }

        await _unitOfWork.Categories.DeleteAsync(category);
        await _unitOfWork.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("{id}/products")]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetCategoryProducts(int id)
    {
        var category = await _unitOfWork.Categories.GetByIdAsync(id);
        if (category == null)
        {
            return NotFound();
        }

        var products = await _unitOfWork.Products.GetByCategoryAsync(id);
        var productDtos = products.Select(p => MapToProductDto(p));
        
        return Ok(productDtos);
    }

    [HttpPost("sync-fakestore")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> SyncWithFakeStoreApi()
    {
        try
        {
            var fakeStoreCategories = await _fakeStoreApi.GetCategoriesAsync();
            var syncedCount = 0;
            
            foreach (var categoryName in fakeStoreCategories)
            {
                // Check if category already exists
                var existingCategory = await _unitOfWork.Categories.GetByNameAsync(categoryName);
                if (existingCategory != null)
                    continue;

                // Create category
                var category = new Category
                {
                    Name = categoryName,
                    Description = $"Products in {categoryName} category",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Categories.AddAsync(category);
                syncedCount++;
            }

            await _unitOfWork.SaveChangesAsync();
            
            return Ok(new { message = "Categories synchronized successfully", count = syncedCount });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error synchronizing categories", error = ex.Message });
        }
    }

    private CategoryDto MapToCategoryDto(Category category)
    {
        return new CategoryDto(
            category.Id,
            category.Name,
            category.Description,
            category.ImageUrl,
            category.IsActive,
            category.Products.Count,
            category.CreatedAt,
            category.UpdatedAt);
    }

    private ProductDto MapToProductDto(Product product)
    {
        var averageRating = product.Reviews.Any() 
            ? product.Reviews.Where(r => r.IsApproved).Average(r => (double)r.Rating)
            : (product.ExternalRating.HasValue ? (double)product.ExternalRating.Value : 0);

        var reviewCount = product.Reviews.Count(r => r.IsApproved) + (product.ExternalRatingCount ?? 0);

        return new ProductDto(
            product.Id,
            product.Title,
            product.Description,
            product.Price,
            product.ImageUrl,
            product.CategoryId,
            product.Category?.Name ?? "Unknown",
            product.IsActive,
            product.StockQuantity,
            averageRating,
            reviewCount,
            product.CreatedAt,
            product.UpdatedAt);
    }
}