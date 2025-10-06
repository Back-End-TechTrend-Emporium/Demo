using Logica.Models.Wishlist;
using System.Security.Claims;

namespace Logica.Interfaces
{
    public interface IWishlistService
    {
        Task<(WishlistResponseDto? response, string? error)> GetWishlistAsync(ClaimsPrincipal user);
        Task<(WishlistOperationResponseDto response, string? error)> AddProductToWishlistAsync(ClaimsPrincipal user, WishlistAddProductDto request);
        Task<(WishlistOperationResponseDto response, string? error)> RemoveProductFromWishlistAsync(ClaimsPrincipal user, WishlistRemoveProductDto request);
        Task<(bool exists, string? error)> ProductExistsInWishlistAsync(ClaimsPrincipal user, Guid productId);
    }
}