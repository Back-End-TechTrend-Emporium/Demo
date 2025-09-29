using Data;
using Data.Entities;

using Logica.Interfaces;
using Logica.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Back_End_TechTrend_Emporium.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IUserService _userService;

        public TestController(AppDbContext context, IUserService userService)
        {
            _context = context;
            _userService = userService;
        }

        [HttpGet("users")]
        public async Task<ActionResult<IEnumerable<GetUserResponse>>> GetUsers()
        {
            var users = await _userService.GetAllUsersAsync();
          

            return Ok(users);
        }

        [HttpPost("users")]
        public async Task<ActionResult<User>> CreateUser(CreateUserRequest request)
        {
            try
            {
                // Usando el servicio con validaciones automáticas
                var user = await _userService.CreateUserAsync(
                    request.Email,
                    request.Username,
                    request.Password,
                    request.Role);

                return CreatedAtAction(nameof(GetUsers), new { id = user.Id }, user);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("categories")]
        public async Task<ActionResult<IEnumerable<Category>>> GetCategories()
        {
            return await _context.Categories.ToListAsync();
        }

        [HttpGet("products")]
        public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
        {
            return await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Creator)
                .ToListAsync();
        }
    }

  
}