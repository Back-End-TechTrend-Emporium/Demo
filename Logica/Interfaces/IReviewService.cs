using Logica.Models.Reviews;

namespace Logica.Interfaces
{
    public interface IReviewService
    {
        Task<ReviewsResponseDto> GetByProductAsync(Guid productId, CancellationToken ct = default);
        Task<ReviewDto> AddAsync(Guid productId, ReviewCreateDto dto, CancellationToken ct = default);
    }
}
