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
                .AsNoTracking()
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Product>> GetByStateAsync(ApprovalState state)
        {
            return await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Creator)
                .Where(p => p.State == state)
                .AsNoTracking()
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<Product?> GetByIdAsync(Guid id)
        {
            return await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Creator)
                .FirstOrDefaultAsync(p => p.Id == id);
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

        public async Task<IEnumerable<Product>> GetByCategoryIdAsync(Guid categoryId)
        {
            return await _context.Products
                .Include(p => p.Category)
                .Where(p => p.CategoryId == categoryId)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Product> CreateAsync(Product product)
        {
            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            return product;
        }

        public async Task<Product> UpdateAsync(Product product)
        {
            product.UpdatedAt = DateTime.UtcNow;
            _context.Products.Update(product);
            await _context.SaveChangesAsync();
            return product;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return false;

            product.State = ApprovalState.Deleted;
            product.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExternalIdExistsAsync(string externalId, ExternalSource source)
        {
            return await _context.ExternalMappings.AnyAsync(em => 
                em.SourceId == externalId && 
                em.Source == source && 
                em.SourceType == "PRODUCT");
        }

        public async Task<int> GetCountAsync()
        {
            return await _context.Products.CountAsync();
        }
    }
}