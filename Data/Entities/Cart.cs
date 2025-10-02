using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Data.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace Data.Entities
{
    [Index(nameof(UserId), nameof(Status))]
    public class Cart
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public CartStatus Status { get; set; } = CartStatus.Active;

        public Guid? AppliedCouponId { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal TotalBeforeDiscount { get; set; } = 0;

        [Column(TypeName = "decimal(10,2)")]
        public decimal DiscountAmount { get; set; } = 0;

        [Column(TypeName = "decimal(10,2)")]
        public decimal ShippingCost { get; set; } = 0;

        [Column(TypeName = "decimal(10,2)")]
        public decimal FinalTotal { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey(nameof(UserId))]
        public User User { get; set; } = null!;

        [ForeignKey(nameof(AppliedCouponId))]
        public Coupon? AppliedCoupon { get; set; }

        public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    }
}