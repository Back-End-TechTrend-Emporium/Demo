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
        #region Fields and Constructor

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

        #endregion

        #region Public Category Operations

        /// <summary>
        /// Get all approved categories (simplified view)
        /// </summary>
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

        /// <summary>
        /// Get specific category by ID
        /// </summary>
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

        /// <summary>
        /// Search categories by term
        /// </summary>
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

        /// <summary>
        /// Get products filtered by category from FakeStore
        /// </summary>
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

        #endregion

        #region Administrative Operations (SuperAdmin/Employee Only)

        /// <summary>
        /// Get all categories (administrative view)
        /// </summary>
        [HttpGet("all")]
        public async Task<ActionResult<IEnumerable<CategoryDto>>> GetAllCategories()
        {
            try
            {
                // Note: Consider adding role-based authorization here if needed
                var categories = await _categoryService.GetAllCategoriesAsync();
                return Ok(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all categories");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Create new category (SuperAdmin/Employee only)
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<CategoryCreateResponseDto>> CreateCategory(CategoryCreateDto categoryDto)
        {
            try
            {
                // Authorization check
                if (!HasAdministrativePermissions())
                {
                    return CreateForbiddenResponse("Only SuperAdmin and Employee can create categories");
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

        /// <summary>
        /// Update existing category (SuperAdmin/Employee only)
        /// </summary>
        [HttpPut]
        public async Task<ActionResult<CategoryResponseDto>> UpdateCategory(CategoryUpdateDto categoryDto)
        {
            try
            {
                // Authorization check
                if (!HasAdministrativePermissions())
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
                    Message = "Updated successfully"
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating category {CategoryId}", categoryDto.Id);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Delete category (SuperAdmin only) or mark for deletion (Employee)
        /// </summary>
        [HttpDelete]
        public async Task<ActionResult<CategoryResponseDto>> DeleteCategory(CategoryDeleteDto deleteDto)
        {
            try
            {
                var currentUserRole = GetCurrentUserRole();
                
                // Basic authorization check
                if (!HasAdministrativePermissions())
                {
                    return Forbid("Only SuperAdmin and Employee can delete categories");
                }

                // SuperAdmin can delete immediately
                if (IsSuperAdmin())
                {
                    var success = await _categoryService.DeleteCategoryAsync(deleteDto.Id);

                    if (!success)
                    {
                        return NotFound($"Category with ID {deleteDto.Id} not found");
                    }

                    return Ok(new CategoryResponseDto { Message = "Deleted successfully" });
                }
                
                // Employee can only mark for deletion
                if (IsEmployee())
                {
                    // TODO: Implement pending deletion logic in service
                    return Ok(new CategoryResponseDto { Message = "Category marked as pending for deletion" });
                }

                return Forbid("Insufficient permissions to delete categories");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting category {CategoryId}", deleteDto.Id);
                return StatusCode(500, "Internal server error");
            }
        }

        #endregion

        #region Approval Workflow Operations

        /// <summary>
        /// Get categories pending approval (SuperAdmin/Employee only)
        /// </summary>
        [HttpGet("pending-approval")]
        public async Task<ActionResult<IEnumerable<CategoryDto>>> GetPendingApproval()
        {
            try
            {
                if (!HasAdministrativePermissions())
                {
                    return Forbid("Only SuperAdmin and Employee can view pending categories");
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

        /// <summary>
        /// Approve category (SuperAdmin/Employee only)
        /// </summary>
        [HttpPost("{id:guid}/approve")]
        public async Task<ActionResult> ApproveCategory(Guid id)
        {
            try
            {
                if (!HasAdministrativePermissions())
                {
                    return Forbid("Only SuperAdmin and Employee can approve categories");
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

        /// <summary>
        /// Reject category (SuperAdmin/Employee only)
        /// </summary>
        [HttpPost("{id:guid}/reject")]
        public async Task<ActionResult> RejectCategory(Guid id)
        {
            try
            {
                if (!HasAdministrativePermissions())
                {
                    return Forbid("Only SuperAdmin and Employee can reject categories");
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

        #endregion

        #region FakeStore Integration Operations

        /// <summary>
        /// Get categories from FakeStore API (without importing)
        /// </summary>
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

        /// <summary>
        /// Synchronize categories from FakeStore API
        /// </summary>
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

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Checks if current user has administrative permissions (SuperAdmin or Employee)
        /// </summary>
        private bool HasAdministrativePermissions()
        {
            var currentUserRole = GetCurrentUserRole();
            return currentUserRole.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase) || 
                   currentUserRole.Equals("Employee", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Checks if current user is SuperAdmin
        /// </summary>
        private bool IsSuperAdmin()
        {
            return GetCurrentUserRole().Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Checks if current user is Employee
        /// </summary>
        private bool IsEmployee()
        {
            return GetCurrentUserRole().Equals("Employee", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Creates a forbidden response with proper error structure
        /// </summary>
        private ActionResult<CategoryCreateResponseDto> CreateForbiddenResponse(string message)
        {
            var forbiddenResponse = new CategoryCreateResponseDto
            {
                CategoryId = Guid.Empty,
                Message = "Failure"
            };
            return Forbid(message);
        }

        #endregion
    }
}