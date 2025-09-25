using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Data.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace Data.Entities
{
    [Index(nameof(Name), IsUnique = true)]
    [Index(nameof(Slug), IsUnique = true)]
    public class Category
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(120)]
        public string Slug { get; set; } = string.Empty;

        [Required]
        public ApprovalState State { get; set; } = ApprovalState.PendingApproval;

        [Required]
        public Guid CreatedBy { get; set; }

        public Guid? ApprovedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public DateTime? ApprovedAt { get; set; }

        // Navigation properties
        [ForeignKey(nameof(CreatedBy))]
        public User Creator { get; set; } = null!;

        [ForeignKey(nameof(ApprovedBy))]
        public User? Approver { get; set; }

        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}