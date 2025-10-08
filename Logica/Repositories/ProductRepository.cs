using Data;
using Data.Entities;
using Data.Entities.Enums;
using Logica.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Logica.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly AppDbContext _context;

        public ProductRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Product>> GetAllAsync()
        {
            return await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Creator)
                .Include(p => p.Approver)
                .AsNoTracking()
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Product>> GetByStateAsync(ApprovalState state)
        {
            return await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Creator)
                .Include(p => p.Approver)
                .Where(p => p.State == state)
                .AsNoTracking()
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Product>> GetByCreatorIdAsync(Guid userId)
        {
            return await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Creator)
                .Include(p => p.Approver)
                .Where(p => p.CreatedBy == userId)
                .AsNoTracking()
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<Product?> GetByIdAsync(Guid id)
        {
            return await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Creator)
                .Include(p => p.Approver)
                .Include(p => p.Reviews)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<Product> CreateAsync(Product product)
        {
            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            return await GetByIdAsync(product.Id) ?? product;
        }

        public async Task<Product> UpdateAsync(Product product)
        {
            product.UpdatedAt = DateTime.UtcNow;
            _context.Products.Update(product);
            await _context.SaveChangesAsync();
            return await GetByIdAsync(product.Id) ?? product;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return false;
            //Avoid deleting already deleted products
            if (product.State == ApprovalState.Deleted)
                throw new InvalidOperationException("Product already deleted.");

            // Soft delete - change state to deleted
            product.State = ApprovalState.Deleted;
            product.UpdatedAt = DateTime.UtcNow;
            _context.Products.Update(product);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _context.Products.AnyAsync(p => p.Id == id);
        }

        public async Task<Product?> GetByExternalIdAsync(string externalId, ExternalSource source)
        {
            var mapping = await _context.ExternalMappings
                .FirstOrDefaultAsync(em => em.SourceId == externalId && 
                                          em.Source == source && 
                                          em.SourceType == "PRODUCT");
            
            if (mapping == null) return null;

            return await GetByIdAsync(mapping.InternalId);
        }

        public async Task<bool> ExternalIdExistsAsync(string externalId, ExternalSource source)
        {
            return await _context.ExternalMappings.AnyAsync(em => 
                em.SourceId == externalId && 
                em.Source == source && 
                em.SourceType == "PRODUCT");
        }

        public async Task<IEnumerable<Product>> GetByCategoryIdAsync(Guid categoryId)
        {
            return await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Creator)
                .Where(p => p.CategoryId == categoryId)
                .AsNoTracking()
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Product>> SearchAsync(string searchTerm)
        {
            return await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Creator)
                .Where(p => p.Title.Contains(searchTerm) || 
                           p.Description!.Contains(searchTerm) ||
                           p.Category.Name.Contains(searchTerm))
                .AsNoTracking()
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<int> GetCountAsync()
        {
            return await _context.Products.CountAsync();
        }

        public async Task<int> GetCountByStateAsync(ApprovalState state)
        {
            return await _context.Products.CountAsync(p => p.State == state);
        }
    }
}