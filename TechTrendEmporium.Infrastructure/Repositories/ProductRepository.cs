using Microsoft.EntityFrameworkCore;
using TechTrendEmporium.Core.Entities;
using TechTrendEmporium.Core.Interfaces;
using TechTrendEmporium.Infrastructure.Data;

namespace TechTrendEmporium.Infrastructure.Repositories;

public class ProductRepository : Repository<Product>, IProductRepository
{
    public ProductRepository(ApplicationDbContext context) : base(context)
    {
    }

    public override async Task<Product?> GetByIdAsync(int id)
    {
        return await _dbSet
            .Include(p => p.Category)
            .Include(p => p.Reviews)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public override async Task<IEnumerable<Product>> GetAllAsync()
    {
        return await _dbSet
            .Include(p => p.Category)
            .Include(p => p.Reviews)
            .Where(p => p.IsActive)
            .ToListAsync();
    }

    public async Task<IEnumerable<Product>> GetByCategoryAsync(int categoryId)
    {
        return await _dbSet
            .Include(p => p.Category)
            .Include(p => p.Reviews)
            .Where(p => p.CategoryId == categoryId && p.IsActive)
            .ToListAsync();
    }

    public async Task<IEnumerable<Product>> SearchAsync(string searchTerm)
    {
        return await _dbSet
            .Include(p => p.Category)
            .Include(p => p.Reviews)
            .Where(p => p.IsActive && 
                (p.Title.Contains(searchTerm) || 
                 p.Description.Contains(searchTerm) ||
                 p.Category.Name.Contains(searchTerm)))
            .ToListAsync();
    }

    public async Task<IEnumerable<Product>> GetFeaturedAsync()
    {
        return await _dbSet
            .Include(p => p.Category)
            .Include(p => p.Reviews)
            .Where(p => p.IsActive)
            .OrderByDescending(p => p.Reviews.Average(r => (double?)r.Rating))
            .Take(10)
            .ToListAsync();
    }

    public async Task<Product?> GetByExternalIdAsync(int externalId)
    {
        return await _dbSet
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.ExternalId == externalId);
    }
}