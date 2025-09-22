using Microsoft.EntityFrameworkCore;
using TechTrendEmporium.Core.Entities;
using TechTrendEmporium.Core.Interfaces;
using TechTrendEmporium.Infrastructure.Data;

namespace TechTrendEmporium.Infrastructure.Repositories;

public class CategoryRepository : Repository<Category>, ICategoryRepository
{
    public CategoryRepository(ApplicationDbContext context) : base(context)
    {
    }

    public override async Task<IEnumerable<Category>> GetAllAsync()
    {
        return await _dbSet
            .Include(c => c.Products)
            .Where(c => c.IsActive)
            .ToListAsync();
    }

    public async Task<Category?> GetByNameAsync(string name)
    {
        return await _dbSet
            .Include(c => c.Products)
            .FirstOrDefaultAsync(c => c.Name == name);
    }

    public async Task<Category?> GetByExternalIdAsync(int externalId)
    {
        return await _dbSet
            .FirstOrDefaultAsync(c => c.ExternalId == externalId);
    }
}