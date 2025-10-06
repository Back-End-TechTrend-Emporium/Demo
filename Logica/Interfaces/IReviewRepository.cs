using Data.Entities;

namespace Logica.Interfaces
{
    public interface IReviewRepository
    {
        Task<List<Review>> GetByProductAsync(Guid productId, CancellationToken ct = default);
        Task<Review> AddAsync(Review review, CancellationToken ct = default);
        Task<bool> ProductExistsAsync(Guid productId, CancellationToken ct = default);
    }
}
