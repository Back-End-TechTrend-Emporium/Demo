using Data.Entities;
using Logica.Models;
using Logica.Models.Category;
using Logica.Models.Products;

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
                Id = product.Id, // ✅ Usar directamente el GUID
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
            
            // Handle Rating updates
            if (updateDto.Rating != null)
            {
                product.RatingAverage = (decimal)updateDto.Rating.Rate;
                product.RatingCount = updateDto.Rating.Count;
            }
            
            // Handle Inventory updates
            if (updateDto.Inventory != null)
            {
                product.InventoryTotal = updateDto.Inventory.Total;
                product.InventoryAvailable = updateDto.Inventory.Available;
            }
            
            product.UpdatedAt = DateTime.UtcNow;
        }

        public static CartDto ToCartDto(this Cart cart)
        {
            return new CartDto
            {
                Id = cart.Id,
                UserId = cart.UserId.ToString(),
                ShoppingCart = cart.CartItems?.Select(ci => ConvertGuidToInt(ci.ProductId)).ToList() ?? new List<int>(),
                CouponApplied = cart.AppliedCoupon != null ? new CouponAppliedDto
                {
                    CouponCode = cart.AppliedCoupon.Code,
                    DiscountPercentage = cart.AppliedCoupon.DiscountPercentage
                } : null,
                TotalBeforeDiscount = cart.TotalBeforeDiscount,
                TotalAfterDiscount = cart.TotalBeforeDiscount - cart.DiscountAmount,
                ShippingCost = cart.ShippingCost,
                FinalTotal = cart.FinalTotal
            };
        }

        private static int ConvertGuidToInt(Guid guid)
        {
            var bytes = guid.ToByteArray();
            return BitConverter.ToInt32(bytes, 0);
        }

        public static ProductSummaryDto ToSummaryDto(Product product)
        {
            return new ProductSummaryDto
            {
                Id = product.Id,
                Title = product.Title,
                Price = product.Price,
                Category = product.Category?.Name ?? "Sin categoría"
            };
        }
    }
}