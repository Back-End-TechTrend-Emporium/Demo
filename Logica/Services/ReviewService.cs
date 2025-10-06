using Data.Entities;
using Logica.Interfaces;
using Logica.Mappers;
using Logica.Models.Reviews;

namespace Logica.Services
{
    public class ReviewService : IReviewService
    {
        private readonly IReviewRepository _reviews;
        private readonly IUserRepository _users;

        public ReviewService(IReviewRepository reviews, IUserRepository users)
        {
            _reviews = reviews;
            _users = users;
        }

        public async Task<ReviewsResponseDto> GetByProductAsync(Guid productId, CancellationToken ct = default)
        {
            var list = await _reviews.GetByProductAsync(productId, ct);
            return new ReviewsResponseDto
            {
                Product_Id = productId,
                Reviews = list.Select(x => x.ToDto()).ToList()
            };
        }

        public async Task<ReviewDto> AddAsync(Guid productId, ReviewCreateDto dto, CancellationToken ct = default)
        {
            // (Opcional pero recomendable) validar que el producto existe
            var exists = await _reviews.ProductExistsAsync(productId, ct);
            if (!exists) throw new KeyNotFoundException($"Product '{productId}' not found.");

            // Convertir username -> UserId
            var user = await _users.GetByUsernameAsync(dto.User, ct);
            if (user is null) throw new KeyNotFoundException($"User '{dto.User}' not found.");

            var entity = new Review
            {
                ProductId = productId,
                UserId = user.Id,
                Rating = dto.Rating,
                Comment = dto.Comment
            };

            var saved = await _reviews.AddAsync(entity, ct);
            return saved.ToDto();
        }
    }
}
