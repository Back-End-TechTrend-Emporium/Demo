using Back_End_TechTrend_Emporium.Abstractions;
using Back_End_TechTrend_Emporium.Models;
using Microsoft.EntityFrameworkCore;

namespace Back_End_TechTrend_Emporium.Data
{
    public sealed class EfTodoRepository : ITodoRepository
    {
        private readonly TodoDbContext _db;
        
        public EfTodoRepository(TodoDbContext db) => _db = db;

        public async Task<IReadOnlyList<TodoItem>> GetAllAsync(CancellationToken ct = default) =>
            await _db.Todos.AsNoTracking().ToListAsync(ct);

        public Task<TodoItem?> GetAsync(Guid id, CancellationToken ct = default) =>
            _db.Todos.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, ct);

        public async Task<TodoItem> AddAsync(TodoItem item, CancellationToken ct = default)
        {
            var entity = item with { Id = item.Id == Guid.Empty ? Guid.NewGuid() : item.Id };
            _db.Todos.Add(entity);
            await _db.SaveChangesAsync(ct);
            return entity;
        }

        public async Task<bool> UpdateAsync(TodoItem item, CancellationToken ct = default)
        {
            var exists = await _db.Todos.AnyAsync(t => t.Id == item.Id, ct);
            if (!exists) return false;
            _db.Todos.Update(item);
            await _db.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var rows = await _db.Todos.Where(t => t.Id == id).ExecuteDeleteAsync(ct);
            return rows > 0;
        }
    }
}