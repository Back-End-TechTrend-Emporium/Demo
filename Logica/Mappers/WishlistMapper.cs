using Data.Entities;
using Logica.Models.Wishlist;

namespace Logica.Mappers
{
    public static class WishlistMapper
    {
        public static WishlistResponseDto ToResponseDto(Wishlist wishlist, bool includeDetails = false)
        {
            var dto = new WishlistResponseDto
            {
                UserId = wishlist.UserId,
                Wishlist = wishlist.WishlistItems.Select(wi => wi.ProductId).ToList()
            };

            if (includeDetails)
            {
                dto.WishlistItems = wishlist.WishlistItems.Select(ToWishlistItemDto).ToList();
            }

            return dto;
        }

        public static WishlistItemDto ToWishlistItemDto(WishlistItem wishlistItem)
        {
            return new WishlistItemDto
            {
                ProductId = wishlistItem.ProductId,
                Title = wishlistItem.Product.Title,
                Price = wishlistItem.Product.Price,
                ImageUrl = wishlistItem.Product.ImageUrl,
                CategoryName = wishlistItem.Product.Category.Name,
                AddedAt = wishlistItem.CreatedAt,
                IsAvailable = wishlistItem.Product.InventoryAvailable > 0
            };
        }

        public static WishlistOperationResponseDto ToOperationResponse(bool success, string message, Wishlist? wishlist = null)
        {
            return new WishlistOperationResponseDto
            {
                Success = success,
                Message = message,
                UpdatedWishlist = wishlist != null ? ToResponseDto(wishlist) : null
            };
        }
    }
}