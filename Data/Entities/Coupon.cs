using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Data.Entities
{
    [Index(nameof(Code), IsUnique = true)]
    public class Coupon
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(20)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(5,2)")]
        [Range(0, 100)]
        public decimal DiscountPercentage { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime? ValidFrom { get; set; }

        public DateTime? ValidTo { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public ICollection<Cart> Carts { get; set; } = new List<Cart>();
    }
}