using Data.Entities;
using Data.Entities.Enums;

namespace Logica.Interfaces
{
    public interface IProductRepository
    {
        Task<IEnumerable<Product>> GetAllAsync();
        Task<IEnumerable<Product>> GetByStateAsync(ApprovalState state);
        Task<Product?> GetByIdAsync(Guid id);
        Task<Product?> GetByExternalIdAsync(string externalId, ExternalSource source);
        Task<IEnumerable<Product>> GetByCategoryIdAsync(Guid categoryId);
        Task<Product> CreateAsync(Product product);
        Task<Product> UpdateAsync(Product product);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> ExternalIdExistsAsync(string externalId, ExternalSource source);
        Task<int> GetCountAsync();
    }
}