using Data.Entities;
using Data.Entities.Enums;

namespace Logica.Interfaces
{
    public interface IProductRepository
    {
        // Basic CRUD
        Task<IEnumerable<Product>> GetAllAsync();
        Task<IEnumerable<Product>> GetByStateAsync(ApprovalState state);
        Task<Product?> GetByIdAsync(Guid id);
        Task<Product> CreateAsync(Product product);
        Task<Product> UpdateAsync(Product product);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> ExistsAsync(Guid id);
        
        // External mapping operations
        Task<Product?> GetByExternalIdAsync(string externalId, ExternalSource source);
        Task<bool> ExternalIdExistsAsync(string externalId, ExternalSource source);
        
        // Category and search operations
        Task<IEnumerable<Product>> GetByCategoryIdAsync(Guid categoryId);
        Task<IEnumerable<Product>> SearchAsync(string searchTerm);
        
        // User-specific operations
        Task<IEnumerable<Product>> GetByCreatorIdAsync(Guid userId);
        
        // Statistics
        Task<int> GetCountAsync();
        Task<int> GetCountByStateAsync(ApprovalState state);
    }
}