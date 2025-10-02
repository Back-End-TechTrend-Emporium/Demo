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

        #region CRUD Operations (Local Database)

        /// <summary>
        /// Obtiene todas las categorías de la base de datos local
        /// </summary>
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
                _logger.LogError(ex, "Error al obtener todas las categorías");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Obtiene solo las categorías aprobadas de la base de datos local
        /// </summary>
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
                _logger.LogError(ex, "Error al obtener categorías aprobadas");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Obtiene una categoría específica por ID de la base de datos local
        /// </summary>
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<CategoryDto>> GetCategory(Guid id)
        {
            try
            {
                var category = await _categoryService.GetCategoryByIdAsync(id);
                
                if (category == null)
                {
                    return NotFound($"Categoría con ID {id} no encontrada");
                }
                
                return Ok(category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener categoría {CategoryId}", id);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Obtiene una categoría específica por slug de la base de datos local
        /// </summary>
        [HttpGet("slug/{slug}")]
        public async Task<ActionResult<CategoryDto>> GetCategoryBySlug(string slug)
        {
            try
            {
                var category = await _categoryService.GetCategoryBySlugAsync(slug);
                
                if (category == null)
                {
                    return NotFound($"Categoría con slug '{slug}' no encontrada");
                }
                
                return Ok(category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener categoría por slug {Slug}", slug);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Crea una nueva categoría en la base de datos local
        /// </summary>
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
                _logger.LogWarning(ex, "Error de validación al crear categoría");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear categoría");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Actualiza una categoría existente en la base de datos local
        /// </summary>
        [HttpPut("{id:guid}")]
        public async Task<ActionResult<CategoryDto>> UpdateCategory(Guid id, CategoryUpdateDto categoryDto)
        {
            try
            {
                var category = await _categoryService.UpdateCategoryAsync(id, categoryDto);

                if (category == null)
                {
                    return NotFound($"Categoría con ID {id} no encontrada");
                }

                return Ok(category);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Error de validación al actualizar categoría {CategoryId}", id);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar categoría {CategoryId}", id);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Elimina una categoría de la base de datos local (hard delete)
        /// </summary>
        [HttpDelete("{id:guid}")]
        public async Task<ActionResult> DeleteCategory(Guid id)
        {
            try
            {
                var success = await _categoryService.DeleteCategoryAsync(id);
                
                if (!success)
                {
                    return NotFound($"Categoría con ID {id} no encontrada");
                }
                
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Error de validación al eliminar categoría {CategoryId}", id);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar categoría {CategoryId}", id);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Busca categorías en la base de datos local
        /// </summary>
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<CategoryDto>>> SearchCategories([FromQuery] string searchTerm)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    return BadRequest("El término de búsqueda es requerido");
                }

                var categories = await _categoryService.SearchCategoriesAsync(searchTerm);
                return Ok(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en búsqueda de categorías");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        #endregion

        #region FakeStore API Operations

        /// <summary>
        /// Obtiene categorías desde FakeStore API
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
                _logger.LogError(ex, "Error al obtener categorías de FakeStore");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        #endregion

        #region Sync Operations

        /// <summary>
        /// Sincroniza categorías desde FakeStore API a la base de datos local
        /// </summary>
        [HttpPost("sync-from-fakestore")]
        public async Task<ActionResult<object>> SyncCategoriesFromFakeStore()
        {
            try
            {
                var createdBy = GetCurrentUserId();
                var importedCount = await _categoryService.SyncCategoriesFromFakeStoreAsync(createdBy);
                
                return Ok(new
                {
                    Message = "Sincronización de categorías completada exitosamente",
                    ImportedCount = importedCount,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en sincronización de categorías desde FakeStore");
                return StatusCode(500, "Error durante la sincronización");
            }
        }

        #endregion

        #region Approval Operations

        /// <summary>
        /// Obtiene categorías pendientes de aprobación
        /// </summary>
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
                _logger.LogError(ex, "Error al obtener categorías pendientes de aprobación");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Aprueba una categoría
        /// </summary>
        [HttpPost("{id:guid}/approve")]
        public async Task<ActionResult> ApproveCategory(Guid id)
        {
            try
            {
                var approvedBy = GetCurrentUserId();
                var success = await _categoryService.ApproveCategoryAsync(id, approvedBy);
                
                if (!success)
                {
                    return NotFound($"Categoría con ID {id} no encontrada o no está pendiente de aprobación");
                }
                
                return Ok(new { Message = "Categoría aprobada exitosamente" });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Error de validación al aprobar categoría {CategoryId}", id);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al aprobar categoría {CategoryId}", id);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Rechaza una categoría
        /// </summary>
        [HttpPost("{id:guid}/reject")]
        public async Task<ActionResult> RejectCategory(Guid id)
        {
            try
            {
                var success = await _categoryService.RejectCategoryAsync(id);
                
                if (!success)
                {
                    return NotFound($"Categoría con ID {id} no encontrada o no está pendiente de aprobación");
                }
                
                return Ok(new { Message = "Categoría rechazada exitosamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al rechazar categoría {CategoryId}", id);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        #endregion
    }
}