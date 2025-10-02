using Logica.Models;
using Logica.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace TechTrendEmporium.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriesController : BaseController
    {
        private readonly ICategoryService _categoryService;
        private readonly ILogger<CategoriesController> _logger;

        public CategoriesController(
            ICategoryService categoryService,
            ILogger<CategoriesController> logger)
        {
            _categoryService = categoryService;
            _logger = logger;
        }

       // CRUD Operations (Local Database)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CategoryDto>>> GetAllCategories()
        {
            try
            {
                var categories = await _categoryService.GetAllCategoriesAsync();
                return Ok(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener todas las categor�as");
                return StatusCode(500, "Error interno del servidor");
            }
        }

       
        [HttpGet("approved")]
        public async Task<ActionResult<IEnumerable<CategoryDto>>> GetApprovedCategories()
        {
            try
            {
                var categories = await _categoryService.GetApprovedCategoriesAsync();
                return Ok(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener categor�as aprobadas");
                return StatusCode(500, "Error interno del servidor");
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
                    return NotFound($"Categor�a con ID {id} no encontrada");
                }
                
                return Ok(category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener categor�a {CategoryId}", id);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        
        [HttpGet("slug/{slug}")]
        public async Task<ActionResult<CategoryDto>> GetCategoryBySlug(string slug)
        {
            try
            {
                var category = await _categoryService.GetCategoryBySlugAsync(slug);
                
                if (category == null)
                {
                    return NotFound($"Categor�a con slug '{slug}' no encontrada");
                }
                
                return Ok(category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener categor�a por slug {Slug}", slug);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpPost]
        public async Task<ActionResult<CategoryDto>> CreateCategory(CategoryCreateDto categoryDto)
        {
            try
            {
                var createdBy = GetCurrentUserId();
                
                var category = await _categoryService.CreateCategoryAsync(categoryDto, createdBy);
                
                return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, category);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Error de validaci�n al crear categor�a");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear categor�a");
                return StatusCode(500, "Error interno del servidor");
            }
        }

       
        [HttpPut("{id:guid}")]
        public async Task<ActionResult<CategoryDto>> UpdateCategory(Guid id, CategoryUpdateDto categoryDto)
        {
            try
            {
                var category = await _categoryService.UpdateCategoryAsync(id, categoryDto);

                if (category == null)
                {
                    return NotFound($"Categor�a con ID {id} no encontrada");
                }

                return Ok(category);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Error de validaci�n al actualizar categor�a {CategoryId}", id);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar categor�a {CategoryId}", id);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        
        [HttpDelete("{id:guid}")]
        public async Task<ActionResult> DeleteCategory(Guid id)
        {
            try
            {
                var success = await _categoryService.DeleteCategoryAsync(id);
                
                if (!success)
                {
                    return NotFound($"Categor�a con ID {id} no encontrada");
                }
                
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Error de validaci�n al eliminar categor�a {CategoryId}", id);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar categor�a {CategoryId}", id);
                return StatusCode(500, "Error interno del servidor");
            }
        }

      
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<CategoryDto>>> SearchCategories([FromQuery] string searchTerm)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    return BadRequest("El t�rmino de b�squeda es requerido");
                }

                var categories = await _categoryService.SearchCategoriesAsync(searchTerm);
                return Ok(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en b�squeda de categor�as");
                return StatusCode(500, "Error interno del servidor");
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
                _logger.LogError(ex, "Error al obtener categor�as de FakeStore");
                return StatusCode(500, "Error interno del servidor");
            }
        }

     

        // Sync Operations

    
        [HttpPost("sync-from-fakestore")]
        public async Task<ActionResult<object>> SyncCategoriesFromFakeStore()
        {
            try
            {
                var createdBy = GetCurrentUserId();
                var importedCount = await _categoryService.SyncCategoriesFromFakeStoreAsync(createdBy);
                
                return Ok(new
                {
                    Message = "Sincronizaci�n de categor�as completada exitosamente",
                    ImportedCount = importedCount,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en sincronizaci�n de categor�as desde FakeStore");
                return StatusCode(500, "Error durante la sincronizaci�n");
            }
        }

        

        // Approval Operations

      
        [HttpGet("pending-approval")]
        public async Task<ActionResult<IEnumerable<CategoryDto>>> GetPendingApproval()
        {
            try
            {
                var categories = await _categoryService.GetPendingApprovalAsync();
                return Ok(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener categor�as pendientes de aprobaci�n");
                return StatusCode(500, "Error interno del servidor");
            }
        }

       
        [HttpPost("{id:guid}/approve")]
        public async Task<ActionResult> ApproveCategory(Guid id)
        {
            try
            {
                var approvedBy = GetCurrentUserId();
                var success = await _categoryService.ApproveCategoryAsync(id, approvedBy);
                
                if (!success)
                {
                    return NotFound($"Categor�a con ID {id} no encontrada o no est� pendiente de aprobaci�n");
                }
                
                return Ok(new { Message = "Categor�a aprobada exitosamente" });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Error de validaci�n al aprobar categor�a {CategoryId}", id);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al aprobar categor�a {CategoryId}", id);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpPost("{id:guid}/reject")]
        public async Task<ActionResult> RejectCategory(Guid id)
        {
            try
            {
                var success = await _categoryService.RejectCategoryAsync(id);
                
                if (!success)
                {
                    return NotFound($"Categor�a con ID {id} no encontrada o no est� pendiente de aprobaci�n");
                }
                
                return Ok(new { Message = "Categor�a rechazada exitosamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al rechazar categor�a {CategoryId}", id);
                return StatusCode(500, "Error interno del servidor");
            }
        }

       

        

      
        private static Guid ConvertIntToGuid(int id)
        {
            var bytes = new byte[16];
            var idBytes = BitConverter.GetBytes(id);
            Array.Copy(idBytes, 0, bytes, 0, 4);
            return new Guid(bytes);
        }

    }
}