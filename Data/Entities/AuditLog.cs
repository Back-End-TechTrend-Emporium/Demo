using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Data.Entities
{
    public class AuditLog
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid? UserId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Action { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string EntityType { get; set; } = string.Empty;

        public Guid? EntityId { get; set; }

        public string? MetadataJson { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }
    }
}