using Data.Entities;
using Logica.Models;

namespace Logica.Mappers
{
    /// <summary>
    /// Mapper para convertir entre Entidades de dominio y DTOs de dominio
    /// </summary>
    public static class ProductMapper
    {
        public static ProductDto ToProductDto(this Product product)
        {
            return new ProductDto
            {
                Id = (int)product.Id.GetHashCode(), // Temporal para compatibilidad con External ID
                Title = product.Title,
                Price = product.Price,
                Description = product.Description ?? string.Empty,
                Category = product.Category?.Name ?? string.Empty,
                Image = product.ImageUrl ?? string.Empty,
                Rating = product.RatingCount > 0 ? new RatingDto
                {
                    Rate = (double)product.RatingAverage,
                    Count = product.RatingCount
                } : null
            };
        }

        public static Product ToProduct(this ProductCreateDto createDto)
        {
            return new Product
            {
                Title = createDto.Title,
                Price = createDto.Price,
                Description = createDto.Description,
                ImageUrl = createDto.Image,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        public static void UpdateProduct(this Product product, ProductUpdateDto updateDto)
        {
            if (!string.IsNullOrEmpty(updateDto.Title))
                product.Title = updateDto.Title;
            
            if (updateDto.Price.HasValue)
                product.Price = updateDto.Price.Value;
            
            if (!string.IsNullOrEmpty(updateDto.Description))
                product.Description = updateDto.Description;
            
            if (!string.IsNullOrEmpty(updateDto.Image))
                product.ImageUrl = updateDto.Image;
            
            product.UpdatedAt = DateTime.UtcNow;
        }

        public static CartDto ToCartDto(this Cart cart)
        {
            return new CartDto
            {
                Id = cart.Id,
                UserId = cart.UserId,
                Status = cart.Status.ToString(),
                TotalBeforeDiscount = cart.TotalBeforeDiscount,
                DiscountAmount = cart.DiscountAmount,
                ShippingCost = cart.ShippingCost,
                FinalTotal = cart.FinalTotal,
                CreatedAt = cart.CreatedAt,
                CouponCode = cart.AppliedCoupon?.Code,
                Products = cart.CartItems?.Select(ci => new CartItemDto
                {
                    ProductId = ci.ProductId,
                    Quantity = ci.Quantity,
                    ProductTitle = ci.TitleSnapshot,
                    UnitPrice = ci.UnitPriceSnapshot,
                    ProductImage = ci.ImageUrlSnapshot,
                    TotalPrice = ci.UnitPriceSnapshot * ci.Quantity
                }).ToList() ?? new List<CartItemDto>()
            };
        }
    }
}