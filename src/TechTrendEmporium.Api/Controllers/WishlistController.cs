using Logica.Interfaces;
using Logica.Models.Wishlist;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TechTrendEmporium.Api.Controllers
{
    [ApiController]
    [Route("api/user")]
    [Authorize(Roles = "Shopper")]
    public class WishlistController : ControllerBase
    {
        private readonly IWishlistService _wishlistService;

        public WishlistController(IWishlistService wishlistService)
        {
            _wishlistService = wishlistService;
        }

        // GET: api/user/wishlist
        [HttpGet("wishlist")]
        public async Task<IActionResult> GetWishlist()
        {
            var (response, error) = await _wishlistService.GetWishlistAsync(User);
            
            if (error != null)
                return BadRequest(new { message = error });

            return Ok(response);
        }

        // POST: api/user/wishlist/add/{product_id}
        [HttpPost("wishlist/add/{productId:guid}")]
        public async Task<IActionResult> AddProductToWishlist(Guid productId)
        {
            var request = new WishlistAddProductDto { ProductId = productId };
            var (response, error) = await _wishlistService.AddProductToWishlistAsync(User, request);

            if (error != null)
                return BadRequest(new { message = error });

            if (!response.Success)
                return BadRequest(new { message = response.Message });

            return Ok(response);
        }

        // DELETE: api/user/wishlist/remove/{product_id}
        [HttpDelete("wishlist/remove/{productId:guid}")]
        public async Task<IActionResult> RemoveProductFromWishlist(Guid productId)
        {
            var request = new WishlistRemoveProductDto { ProductId = productId };
            var (response, error) = await _wishlistService.RemoveProductFromWishlistAsync(User, request);

            if (error != null)
                return BadRequest(new { message = error });

            if (!response.Success)
                return BadRequest(new { message = response.Message });

            return Ok(response);
        }

        // GET: api/user/wishlist/check/{product_id}
        [HttpGet("wishlist/check/{productId:guid}")]
        public async Task<IActionResult> CheckProductInWishlist(Guid productId)
        {
            var (exists, error) = await _wishlistService.ProductExistsInWishlistAsync(User, productId);

            if (error != null)
                return BadRequest(new { message = error });

            return Ok(new { exists });
        }
    }
}