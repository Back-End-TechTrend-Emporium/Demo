using System.ComponentModel.DataAnnotations;
using Data.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace Data.Entities
{
    [Index(nameof(Email), IsUnique = true)]
    [Index(nameof(Username), IsUnique = true)]
    public class User
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(255)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        public Role Role { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public DateTime? LastLoginAt { get; set; }

        // Navigation properties
        public ICollection<Session> Sessions { get; set; } = new List<Session>();
        public Wishlist? Wishlist { get; set; }
        public ICollection<Cart> Carts { get; set; } = new List<Cart>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
        public ICollection<Product> CreatedProducts { get; set; } = new List<Product>();
        public ICollection<Product> ApprovedProducts { get; set; } = new List<Product>();
        public ICollection<Category> CreatedCategories { get; set; } = new List<Category>();
        public ICollection<Category> ApprovedCategories { get; set; } = new List<Category>();
        public ICollection<ApprovalJob> RequestedJobs { get; set; } = new List<ApprovalJob>();
        public ICollection<ApprovalJob> ReviewedJobs { get; set; } = new List<ApprovalJob>();
    }
}