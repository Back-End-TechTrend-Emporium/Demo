namespace Logica.Models.Wishlist
{
    public class WishlistItemDto
    {
        public Guid ProductId { get; set; }
        public string Title { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string? ImageUrl { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public DateTime AddedAt { get; set; }
        public bool IsAvailable { get; set; } = true;
    }
}