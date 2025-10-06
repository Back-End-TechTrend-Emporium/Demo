using Data.Entities;
using Data.Entities.Enums;
using Logica.Interfaces;
using Logica.Mappers;
using Logica.Models.Wishlist;
using System.Security.Claims;

namespace Logica.Services
{
    public class WishlistService : IWishlistService
    {
        private readonly IWishlistRepository _wishlistRepository;
        private readonly IProductRepository _productRepository;

        public WishlistService(IWishlistRepository wishlistRepository, IProductRepository productRepository)
        {
            _wishlistRepository = wishlistRepository;
            _productRepository = productRepository;
        }

        public async Task<(WishlistResponseDto? response, string? error)> GetWishlistAsync(ClaimsPrincipal user)
        {
            try
            {
                var userId = GetUserIdFromClaims(user);
                if (userId == null)
                    return (null, "Usuario no autenticado.");

                var wishlist = await _wishlistRepository.GetByUserIdAsync(userId.Value);
                
                if (wishlist == null)
                {
                    // Crear wishlist vacía si no existe
                    wishlist = new Wishlist { UserId = userId.Value };
                    wishlist = await _wishlistRepository.CreateAsync(wishlist);
                }

                var response = WishlistMapper.ToResponseDto(wishlist, includeDetails: true);
                return (response, null);
            }
            catch (Exception ex)
            {
                return (null, $"Error al obtener la wishlist: {ex.Message}");
            }
        }

        public async Task<(WishlistOperationResponseDto response, string? error)> AddProductToWishlistAsync(ClaimsPrincipal user, WishlistAddProductDto request)
        {
            try
            {
                var userId = GetUserIdFromClaims(user);
                if (userId == null)
                    return (WishlistMapper.ToOperationResponse(false, "Usuario no autenticado."), null);

                // Verificar que el producto existe y está disponible
                var product = await _productRepository.GetByIdAsync(request.ProductId);
                if (product == null)
                    return (WishlistMapper.ToOperationResponse(false, "El producto no existe."), null);

                if (product.State != ApprovalState.Approved)
                    return (WishlistMapper.ToOperationResponse(false, "El producto no está disponible."), null);

                // Verificar si ya existe en la wishlist
                var existsInWishlist = await _wishlistRepository.ProductExistsInWishlistAsync(userId.Value, request.ProductId);
                if (existsInWishlist)
                    return (WishlistMapper.ToOperationResponse(false, "El producto ya está en tu wishlist."), null);

                // Obtener o crear wishlist
                var wishlist = await _wishlistRepository.GetByUserIdAsync(userId.Value);
                if (wishlist == null)
                {
                    wishlist = new Wishlist { UserId = userId.Value };
                    wishlist = await _wishlistRepository.CreateAsync(wishlist);
                }

                // Agregar producto a la wishlist
                var wishlistItem = new WishlistItem
                {
                    WishlistId = wishlist.Id,
                    ProductId = request.ProductId
                };

                await _wishlistRepository.AddItemAsync(wishlistItem);

                // Obtener wishlist actualizada
                wishlist = await _wishlistRepository.GetByUserIdAsync(userId.Value);

                var response = WishlistMapper.ToOperationResponse(true, "Producto agregado a tu wishlist exitosamente.", wishlist);
                return (response, null);
            }
            catch (Exception ex)
            {
                return (WishlistMapper.ToOperationResponse(false, $"Error al agregar producto: {ex.Message}"), null);
            }
        }

        public async Task<(WishlistOperationResponseDto response, string? error)> RemoveProductFromWishlistAsync(ClaimsPrincipal user, WishlistRemoveProductDto request)
        {
            try
            {
                var userId = GetUserIdFromClaims(user);
                if (userId == null)
                    return (WishlistMapper.ToOperationResponse(false, "Usuario no autenticado."), null);

                var wishlist = await _wishlistRepository.GetByUserIdAsync(userId.Value);
                if (wishlist == null)
                    return (WishlistMapper.ToOperationResponse(false, "No tienes una wishlist."), null);

                var wishlistItem = await _wishlistRepository.GetWishlistItemAsync(wishlist.Id, request.ProductId);
                if (wishlistItem == null)
                    return (WishlistMapper.ToOperationResponse(false, "El producto no está en tu wishlist."), null);

                await _wishlistRepository.RemoveItemAsync(wishlistItem);

                // Obtener wishlist actualizada
                wishlist = await _wishlistRepository.GetByUserIdAsync(userId.Value);

                var response = WishlistMapper.ToOperationResponse(true, "Producto removido de tu wishlist exitosamente.", wishlist);
                return (response, null);
            }
            catch (Exception ex)
            {
                return (WishlistMapper.ToOperationResponse(false, $"Error al remover producto: {ex.Message}"), null);
            }
        }

        public async Task<(bool exists, string? error)> ProductExistsInWishlistAsync(ClaimsPrincipal user, Guid productId)
        {
            try
            {
                var userId = GetUserIdFromClaims(user);
                if (userId == null)
                    return (false, "Usuario no autenticado.");

                var exists = await _wishlistRepository.ProductExistsInWishlistAsync(userId.Value, productId);
                return (exists, null);
            }
            catch (Exception ex)
            {
                return (false, $"Error al verificar producto en wishlist: {ex.Message}");
            }
        }

        //IA debug - Mejorar diagnóstico de autenticación
        private Guid? GetUserIdFromClaims(ClaimsPrincipal user)
        {
            // Debug: verificar si el usuario está autenticado
            if (user?.Identity?.IsAuthenticated != true)
                return null;

            // Intentar múltiples formas de obtener el user ID
            var userIdClaim = user.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.NameId)
                           ?? user.FindFirst(ClaimTypes.NameIdentifier)
                           ?? user.FindFirst("nameid")
                           ?? user.FindFirst("sub");

            if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
                return userId;

            return null;
        }
        //IA debug - FIN mejora diagnóstico
    }
}