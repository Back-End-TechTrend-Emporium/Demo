namespace Logica.Models.Wishlist
{
    public class WishlistOperationResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public WishlistResponseDto? UpdatedWishlist { get; set; }
    }
}