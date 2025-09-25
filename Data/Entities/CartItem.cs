using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Data.Entities
{
    [Index(nameof(CartId))]
    [Index(nameof(CartId), nameof(ProductId), IsUnique = true)]
    public class CartItem
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid CartId { get; set; }

        [Required]
        public Guid ProductId { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal UnitPriceSnapshot { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        [MaxLength(200)]
        public string? TitleSnapshot { get; set; }

        [MaxLength(500)]
        public string? ImageUrlSnapshot { get; set; }

        [MaxLength(100)]
        public string? CategoryNameSnapshot { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey(nameof(CartId))]
        public Cart Cart { get; set; } = null!;

        [ForeignKey(nameof(ProductId))]
        public Product Product { get; set; } = null!;
    }
}