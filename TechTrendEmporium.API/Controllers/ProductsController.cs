using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechTrendEmporium.Core.DTOs;
using TechTrendEmporium.Core.Entities;
using TechTrendEmporium.Core.Interfaces;

namespace TechTrendEmporium.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFakeStoreApiService _fakeStoreApi;

    public ProductsController(IUnitOfWork unitOfWork, IFakeStoreApiService fakeStoreApi)
    {
        _unitOfWork = unitOfWork;
        _fakeStoreApi = fakeStoreApi;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetProducts(
        [FromQuery] int? categoryId = null,
        [FromQuery] decimal? minPrice = null,
        [FromQuery] decimal? maxPrice = null,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var products = await _unitOfWork.Products.GetAllAsync();
        
        // Apply filters
        if (categoryId.HasValue)
        {
            products = products.Where(p => p.CategoryId == categoryId.Value);
        }
        
        if (minPrice.HasValue)
        {
            products = products.Where(p => p.Price >= minPrice.Value);
        }
        
        if (maxPrice.HasValue)
        {
            products = products.Where(p => p.Price <= maxPrice.Value);
        }
        
        if (!string.IsNullOrEmpty(search))
        {
            products = products.Where(p => 
                p.Title.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                p.Description.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        // Apply pagination
        var paginatedProducts = products
            .Skip((page - 1) * pageSize)
            .Take(pageSize);

        var productDtos = paginatedProducts.Select(p => MapToProductDto(p));
        
        return Ok(productDtos);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ProductDto>> GetProduct(int id)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(id);
        if (product == null)
        {
            return NotFound();
        }

        return Ok(MapToProductDto(product));
    }

    [HttpPost]
    [Authorize(Policy = "EmployeeOnly")]
    public async Task<ActionResult<ProductDto>> CreateProduct([FromBody] CreateProductRequest request)
    {
        // Check if category exists
        var category = await _unitOfWork.Categories.GetByIdAsync(request.CategoryId);
        if (category == null)
        {
            return BadRequest("Invalid category ID");
        }

        var product = new Product
        {
            Title = request.Title,
            Description = request.Description,
            Price = request.Price,
            ImageUrl = request.ImageUrl,
            CategoryId = request.CategoryId,
            StockQuantity = request.StockQuantity,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Products.AddAsync(product);
        await _unitOfWork.SaveChangesAsync();

        // Reload with category
        product = await _unitOfWork.Products.GetByIdAsync(product.Id);
        
        return CreatedAtAction(nameof(GetProduct), new { id = product!.Id }, MapToProductDto(product));
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "EmployeeOnly")]
    public async Task<ActionResult<ProductDto>> UpdateProduct(int id, [FromBody] UpdateProductRequest request)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(id);
        if (product == null)
        {
            return NotFound();
        }

        // Check if category exists
        var category = await _unitOfWork.Categories.GetByIdAsync(request.CategoryId);
        if (category == null)
        {
            return BadRequest("Invalid category ID");
        }

        product.Title = request.Title;
        product.Description = request.Description;
        product.Price = request.Price;
        product.ImageUrl = request.ImageUrl;
        product.CategoryId = request.CategoryId;
        product.StockQuantity = request.StockQuantity;
        product.IsActive = request.IsActive;
        product.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Products.UpdateAsync(product);
        await _unitOfWork.SaveChangesAsync();

        // Reload with category
        product = await _unitOfWork.Products.GetByIdAsync(product.Id);
        
        return Ok(MapToProductDto(product!));
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(id);
        if (product == null)
        {
            return NotFound();
        }

        await _unitOfWork.Products.DeleteAsync(product);
        await _unitOfWork.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("sync-fakestore")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> SyncWithFakeStoreApi()
    {
        try
        {
            var fakeStoreProducts = await _fakeStoreApi.GetProductsAsync();
            
            foreach (var fakeProduct in fakeStoreProducts)
            {
                // Check if product already exists
                var existingProduct = await _unitOfWork.Products.GetByExternalIdAsync(fakeProduct.Id);
                if (existingProduct != null)
                    continue;

                // Find or create category
                var category = await _unitOfWork.Categories.GetByNameAsync(fakeProduct.Category);
                if (category == null)
                {
                    category = new Category
                    {
                        Name = fakeProduct.Category,
                        Description = $"Products in {fakeProduct.Category} category",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    await _unitOfWork.Categories.AddAsync(category);
                    await _unitOfWork.SaveChangesAsync();
                }

                // Create product
                var product = new Product
                {
                    Title = fakeProduct.Title,
                    Description = fakeProduct.Description,
                    Price = fakeProduct.Price,
                    ImageUrl = fakeProduct.Image,
                    CategoryId = category.Id,
                    ExternalId = fakeProduct.Id,
                    ExternalRating = fakeProduct.Rating.Rate,
                    ExternalRatingCount = fakeProduct.Rating.Count,
                    StockQuantity = 10, // Default stock
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Products.AddAsync(product);
            }

            await _unitOfWork.SaveChangesAsync();
            
            return Ok(new { message = "Products synchronized successfully", count = fakeStoreProducts.Count() });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error synchronizing products", error = ex.Message });
        }
    }

    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<ProductDto>>> SearchProducts([FromQuery] string term)
    {
        if (string.IsNullOrEmpty(term))
        {
            return BadRequest("Search term is required");
        }

        var products = await _unitOfWork.Products.SearchAsync(term);
        var productDtos = products.Select(p => MapToProductDto(p));
        
        return Ok(productDtos);
    }

    [HttpGet("featured")]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetFeaturedProducts()
    {
        var products = await _unitOfWork.Products.GetFeaturedAsync();
        var productDtos = products.Select(p => MapToProductDto(p));
        
        return Ok(productDtos);
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