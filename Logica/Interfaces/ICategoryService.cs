using Logica.Models.Category;

namespace Logica.Interfaces
{
    public interface ICategoryService
    {
        // CRUD Operations
        Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync();
        Task<IEnumerable<CategoryDto>> GetApprovedCategoriesAsync();
        Task<CategoryDto?> GetCategoryByIdAsync(Guid id);
        Task<CategoryDto?> GetCategoryBySlugAsync(string slug);
        Task<CategoryDto> CreateCategoryAsync(CategoryCreateDto categoryDto, Guid createdBy);
        Task<CategoryDto?> UpdateCategoryAsync(Guid id, CategoryUpdateDto categoryDto);
        Task<bool> DeleteCategoryAsync(Guid id);
        
        // Search Operations
        Task<IEnumerable<CategoryDto>> SearchCategoriesAsync(string searchTerm);
        
        // Approval Operations
        Task<IEnumerable<CategoryDto>> GetPendingApprovalAsync();
        Task<bool> ApproveCategoryAsync(Guid id, Guid approvedBy);
        Task<bool> RejectCategoryAsync(Guid id);
        
        // FakeStore API Operations (externos)
        Task<IEnumerable<string>> GetCategoriesFromFakeStoreAsync();
        Task<int> SyncCategoriesFromFakeStoreAsync(Guid createdBy);
    }
}