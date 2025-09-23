using Back_End_TechTrend_Emporium.Models;

namespace Back_End_TechTrend_Emporium.Abstractions
{
    public interface ITodoRepository
    {
        Task<IReadOnlyList<TodoItem>> GetAllAsync(CancellationToken ct = default);
        Task<TodoItem?> GetAsync(Guid id, CancellationToken ct = default);
        Task<TodoItem> AddAsync(TodoItem item, CancellationToken ct = default);
        Task<bool> UpdateAsync(TodoItem item, CancellationToken ct = default);
        Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
    }
}