using Logica.Interfaces;
using Logica.Models.Category;
using Logica.Models.Products;
using Microsoft.AspNetCore.Mvc;

namespace TechTrendEmporium.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriesController : BaseController
    {
        private readonly ICategoryService _categoryService;
        private readonly IProductService _productService;
        private readonly ILogger<CategoriesController> _logger;

        public CategoriesController(
            ICategoryService categoryService,
            IProductService productService,
            ILogger<CategoriesController> logger)
        {
            _categoryService = categoryService;
            _productService = productService;
            _logger = logger;
        }

       

       
        [HttpGet("store/products")]
        public async Task<ActionResult<CategoryFilterResponseDto>> GetProductsByCategory([FromQuery] string category)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(category))
                {
                    return BadRequest("Category parameter is required");
                }

                var products = await _productService.GetProductsByCategoryFromFakeStoreAsync(category);
                
                var response = new CategoryFilterResponseDto
                {
                    SelectedCategory = category,
                    FilteredProducts = products
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error filtering products by category {Category}", category);
                return StatusCode(500, "Internal server error");
            }
        }

      
        [HttpPost]
        public async Task<ActionResult<CategoryCreateResponseDto>> CreateCategory(CategoryCreateDto categoryDto)
        {
            try
            {
                // Verificar que solo SuperAdmin o Employee puedan crear categorías
                var currentUserRole = GetCurrentUserRole();
                if (!currentUserRole.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase) && 
                    !currentUserRole.Equals("Employee", StringComparison.OrdinalIgnoreCase))
                {
                    var forbiddenResponse = new CategoryCreateResponseDto
                    {
                        CategoryId = Guid.Empty,
                        Message = "Failure"
                    };
                    return Forbid("Only SuperAdmin and Employee can create categories");
                }

                var createdBy = GetCurrentUserId();
                var category = await _categoryService.CreateCategoryAsync(categoryDto, createdBy);

                var response = new CategoryCreateResponseDto
                {
                    CategoryId = category.Id,
                    Message = "Successful"
                };

                return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating category");
                
                var errorResponse = new CategoryCreateResponseDto
                {
                    CategoryId = Guid.Empty,
                    Message = "Failure"
                };

                return StatusCode(500, errorResponse);
            }
        }

      
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CategorySimpleDto>>> GetCategories()
        {
            try
            {
                var categories = await _categoryService.GetApprovedCategoriesAsync();
                var simplifiedCategories = categories.Select(c => new CategorySimpleDto
                {
                    Id = c.Id,
                    Name = c.Name
                });

                return Ok(simplifiedCategories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting categories");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<CategoryDto>> GetCategory(Guid id)
        {
            try
            {
                var category = await _categoryService.GetCategoryByIdAsync(id);
                
                if (category == null)
                {
                    return NotFound($"Category with ID {id} not found");
                }

                return Ok(category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting category {CategoryId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

       
        [HttpPut]
        public async Task<ActionResult<CategoryResponseDto>> UpdateCategory(CategoryUpdateDto categoryDto)
        {
            try
            {
                // Verificar que solo SuperAdmin o Employee puedan editar categorías
                var currentUserRole = GetCurrentUserRole();
                if (!currentUserRole.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase) && 
                    !currentUserRole.Equals("Employee", StringComparison.OrdinalIgnoreCase))
                {
                    return Forbid("Only SuperAdmin and Employee can update categories");
                }

                var category = await _categoryService.UpdateCategoryAsync(categoryDto.Id, categoryDto);

                if (category == null)
                {
                    return NotFound($"Category with ID {categoryDto.Id} not found");
                }

                var response = new CategoryResponseDto
                {
                    Message = "Updated successfuly"
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating category {CategoryId}", categoryDto.Id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpDelete]
        public async Task<ActionResult<CategoryResponseDto>> DeleteCategory(CategoryDeleteDto deleteDto)
        {
            try
            {
                var currentUserRole = GetCurrentUserRole();
                
               
                if (!currentUserRole.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase) && 
                    !currentUserRole.Equals("Employee", StringComparison.OrdinalIgnoreCase))
                {
                    return Forbid("Only SuperAdmin and Employee can delete categories");
                }

               
                if (currentUserRole.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase))
                {
                    var success = await _categoryService.DeleteCategoryAsync(deleteDto.Id);

                    if (!success)
                    {
                        return NotFound($"Category with ID {deleteDto.Id} not found");
                    }

                    var response = new CategoryResponseDto
                    {
                        Message = "Deleted successfuly"                     
                    };

                    return Ok(response);
                }
               
                else if (currentUserRole.Equals("Employee", StringComparison.OrdinalIgnoreCase))
                {
                    
                    var response = new CategoryResponseDto
                    {
                        Message = "Category marked as pending for deletion"
                    };

                    return Ok(response);
                }

                
                return Forbid("Insufficient permissions to delete categories");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting category {CategoryId}", deleteDto.Id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("all")]
        public async Task<ActionResult<IEnumerable<CategoryDto>>> GetAllCategories()
        {
            try
            {
       
                var categories = await _categoryService.GetAllCategoriesAsync();
                return Ok(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all categories");
                return StatusCode(500, "Internal server error");
            }
        }

       
        [HttpGet("pending-approval")]
        public async Task<ActionResult<IEnumerable<CategoryDto>>> GetPendingApproval()
        {
            try
            {
                var currentUserRole = GetCurrentUserRole();
                if (!currentUserRole.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase) &&
                    !currentUserRole.Equals("Employee", StringComparison.OrdinalIgnoreCase))
                {
                    return Forbid("Only SuperAdmin and Employee can update categories");
                }

                

                var categories = await _categoryService.GetPendingApprovalAsync();
                return Ok(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending approval categories");
                return StatusCode(500, "Internal server error");
            }
        }

       
        [HttpPost("{id:guid}/approve")]
        public async Task<ActionResult> ApproveCategory(Guid id)
        {
            try
            {
                var currentUserRole = GetCurrentUserRole();
                if (!currentUserRole.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase) &&
                    !currentUserRole.Equals("Employee", StringComparison.OrdinalIgnoreCase))
                {
                    return Forbid("Only SuperAdmin and Employee can update categories");
                }

                var approvedBy = GetCurrentUserId();
                var success = await _categoryService.ApproveCategoryAsync(id, approvedBy);

                if (!success)
                {
                    return NotFound($"Category with ID {id} not found");
                }

                return Ok(new { Message = "Category approved successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving category {CategoryId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

       
        [HttpPost("{id:guid}/reject")]
        public async Task<ActionResult> RejectCategory(Guid id)
        {
            try
            {
                var currentUserRole = GetCurrentUserRole();
                if (!currentUserRole.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase) &&
                    !currentUserRole.Equals("Employee", StringComparison.OrdinalIgnoreCase))
                {
                    return Forbid("Only SuperAdmin and Employee can update categories");
                }

                var success = await _categoryService.RejectCategoryAsync(id);

                if (!success)
                {
                    return NotFound($"Category with ID {id} not found");
                }

                return Ok(new { Message = "Category rejected successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting category {CategoryId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

       
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<CategoryDto>>> SearchCategories([FromQuery] string searchTerm)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    return BadRequest("Search term is required");
                }

                var categories = await _categoryService.SearchCategoriesAsync(searchTerm);
                return Ok(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching categories");
                return StatusCode(500, "Internal server error");
            }
        }

       
        [HttpGet("fakestore")]
        public async Task<ActionResult<IEnumerable<string>>> GetCategoriesFromFakeStore()
        {
            try
            {
                var categories = await _categoryService.GetCategoriesFromFakeStoreAsync();
                return Ok(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting categories from FakeStore");
                return StatusCode(500, "Internal server error");
            }
        }

      
        [HttpPost("sync-from-fakestore")]
        public async Task<ActionResult<object>> SyncCategoriesFromFakeStore()
        {
            try
            {
                var createdBy = GetCurrentUserId();
                var syncCount = await _categoryService.SyncCategoriesFromFakeStoreAsync(createdBy);

                return Ok(new
                {
                    Message = "Categories synchronized successfully",
                    SyncedCount = syncCount,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing categories from FakeStore");
                return StatusCode(500, "Error during synchronization");
            }
        }

      
    }
}