/*
using Back_End_TechTrend_Emporium.Abstractions;
using Back_End_TechTrend_Emporium.Models;
using Microsoft.AspNetCore.Mvc;

namespace Back_End_TechTrend_Emporium.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TodoController : ControllerBase
    {
        private readonly ITodoRepository _todoRepository;
        private readonly ILogger<TodoController> _logger;

        public TodoController(ITodoRepository todoRepository, ILogger<TodoController> logger)
        {
            _todoRepository = todoRepository;
            _logger = logger;
        }

        [HttpPost]
        public async Task<ActionResult<TodoItem>> Create([FromBody] TodoItem item, CancellationToken ct)
        {
            using (_logger.BeginScope(new Dictionary<string, object?>
            {
                ["todoTitle"] = item.Title,
                ["corrId"] = HttpContext.TraceIdentifier
            }))
            {
                _logger.LogInformation("Creating todo item");
                
                if (string.IsNullOrWhiteSpace(item.Title))
                {
                    _logger.LogWarning("Rejected empty title");
                    return BadRequest("Title required");
                }

                var created = await _todoRepository.AddAsync(item, ct);
                
                _logger.LogInformation("Todo created with {TodoId}", created.Id);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TodoItem>> GetById(Guid id, CancellationToken ct)
        {
            var todo = await _todoRepository.GetAsync(id, ct);
            if (todo == null)
            {
                return NotFound();
            }
            return Ok(todo);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TodoItem>>> GetAll(CancellationToken ct)
        {
            var todos = await _todoRepository.GetAllAsync(ct);
            return Ok(todos);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<TodoItem>> Update(Guid id, [FromBody] TodoItem item, CancellationToken ct)
        {
            // Ensure the item's Id matches the route id
            var updatedItem = item with { Id = id };
            var success = await _todoRepository.UpdateAsync(updatedItem, ct);
            if (!success)
            {
                return NotFound();
            }
            return Ok(updatedItem);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(Guid id, CancellationToken ct)
        {
            var deleted = await _todoRepository.DeleteAsync(id, ct);
            if (!deleted)
            {
                return NotFound();
            }
            return NoContent();
        }
    }
}
*/