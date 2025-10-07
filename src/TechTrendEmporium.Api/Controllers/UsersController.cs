using Data.Entities.Enums;
using Logica.Interfaces;
using Logica.Models;
using Microsoft.AspNetCore.Mvc;

namespace TechTrendEmporium.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : BaseController
    {
        private readonly IUserService _userService;
        private readonly ILogger<UsersController> _logger;

        public UsersController(
            IUserService userService,
            ILogger<UsersController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        // Local User Operations
        
        [HttpGet]
        public async Task<ActionResult<IEnumerable<GetUserResponse>>> GetAllUsers()
        {
            try
            {
                var users = await _userService.GetAllUsersAsync();
                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener usuarios");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<GetUserResponse>> GetUser(Guid id)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(id);

                if (user == null)
                {
                    return NotFound($"Usuario con ID {id} no encontrado");
                }

                var response = new GetUserResponse
                {
                    Id = user.Id,
                    Email = user.Email,
                    Username = user.Username
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener usuario {UserId}", id);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpPost]
        public async Task<ActionResult<GetUserResponse>> CreateUser(CreateUserRequest request)
        {
            try
            {
                var user = await _userService.CreateUserAsync(
                    request.Email, 
                    request.Username, 
                    request.Password, 
                    request.Role);

                var response = new GetUserResponse
                {
                    Id = user.Id,
                    Email = user.Email,
                    Username = user.Username
                };

                return CreatedAtAction(nameof(GetUser), new { id = user.Id }, response);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear usuario");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        // FakeStore Operations

        [HttpGet("fakestore")]
        public async Task<ActionResult<IEnumerable<GetUserResponse>>> GetUsersFromFakeStore()
        {
            try
            {
                var users = await _userService.GetUsersFromFakeStoreAsync();
                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener usuarios de FakeStore");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpGet("fakestore/{id:int}")]
        public async Task<ActionResult<GetUserResponse>> GetUserFromFakeStore(int id)
        {
            try
            {
                var user = await _userService.GetUserFromFakeStoreAsync(id);

                if (user == null)
                {
                    return NotFound($"Usuario con ID {id} no encontrado en FakeStore");
                }

                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener usuario {UserId} de FakeStore", id);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        // Sync Operations

        [HttpPost("sync-from-fakestore")]
        public async Task<ActionResult<object>> SyncAllUsersFromFakeStore()
        {
            try
            {
                var importedCount = await _userService.SyncAllUsersFromFakeStoreAsync();

                return Ok(new
                {
                    Message = "Sincronización de usuarios completada exitosamente",
                    ImportedCount = importedCount,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en sincronización de usuarios desde FakeStore");
                return StatusCode(500, "Error durante la sincronización");
            }
        }

        [HttpPost("import-from-fakestore/{fakeStoreId:int}")]
        public async Task<ActionResult<GetUserResponse>> ImportUserFromFakeStore(int fakeStoreId)
        {
            try
            {
                var user = await _userService.ImportUserFromFakeStoreAsync(fakeStoreId);

                if (user == null)
                {
                    return NotFound($"Usuario con ID {fakeStoreId} no encontrado en FakeStore");
                }

                return Ok(user);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importando usuario {UserId} desde FakeStore", fakeStoreId);
                return StatusCode(500, "Error durante la importación");
            }
        }

        // Utility methods (similar to ProductsController)

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