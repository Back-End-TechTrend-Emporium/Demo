using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Data.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace Data.Entities
{
    [Index(nameof(Title))]
    [Index(nameof(Price))]
    [Index(nameof(CategoryId), nameof(State))]
    public class Product
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Price { get; set; }

        public string? Description { get; set; }

        [Required]
        public Guid CategoryId { get; set; }

        [MaxLength(500)]
        public string? ImageUrl { get; set; }

        [Required]
        public ApprovalState State { get; set; } = ApprovalState.PendingApproval;

        [Column(TypeName = "decimal(2,1)")]
        public decimal RatingAverage { get; set; } = 0.0m;

        public int RatingCount { get; set; } = 0; 

        public int InventoryTotal { get; set; } = 0;

        public int InventoryAvailable { get; set; } = 0;

        [Required]
        public Guid CreatedBy { get; set; }

        public Guid? ApprovedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public DateTime? ApprovedAt { get; set; }

        // Navigation properties
        [ForeignKey(nameof(CategoryId))]
        public Category Category { get; set; } = null!;

        [ForeignKey(nameof(CreatedBy))]
        public User Creator { get; set; } = null!;

        [ForeignKey(nameof(ApprovedBy))]
        public User? Approver { get; set; }

        public ICollection<Review> Reviews { get; set; } = new List<Review>();
        public ICollection<WishlistItem> WishlistItems { get; set; } = new List<WishlistItem>();
        public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    }
}