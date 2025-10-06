using Data;
using Data.Entities;
using Logica.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Logica.Repositories
{
    public class ReviewRepository : IReviewRepository
    {
        private readonly AppDbContext _db;
        public ReviewRepository(AppDbContext db) => _db = db;

        public Task<List<Review>> GetByProductAsync(Guid productId, CancellationToken ct = default) =>
            _db.Reviews
               .AsNoTracking()
               .Include(r => r.User) // para Username en DTO
               .Where(r => r.ProductId == productId)
               .OrderByDescending(r => r.CreatedAt)
               .ToListAsync(ct);

        public async Task<Review> AddAsync(Review review, CancellationToken ct = default)
        {
            _db.Reviews.Add(review);
            await _db.SaveChangesAsync(ct);
            // recargar con User para devolver Username
            await _db.Entry(review).Reference(r => r.User).LoadAsync(ct);
            return review;
        }

        public Task<bool> ProductExistsAsync(Guid productId, CancellationToken ct = default) =>
            _db.Products.AnyAsync(p => p.Id == productId, ct);
    }
}
