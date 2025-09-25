using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Data.Entities
{
    [Index(nameof(WishlistId))]
    [Index(nameof(WishlistId), nameof(ProductId), IsUnique = true)]
    public class WishlistItem
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid WishlistId { get; set; }

        [Required]
        public Guid ProductId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey(nameof(WishlistId))]
        public Wishlist Wishlist { get; set; } = null!;

        [ForeignKey(nameof(ProductId))]
        public Product Product { get; set; } = null!;
    }
}