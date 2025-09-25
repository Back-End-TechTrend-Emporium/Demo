using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Data.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace Data.Entities
{
    [Index(nameof(UserId))]
    public class Session
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public SessionStatus Status { get; set; } = SessionStatus.Active;

        [MaxLength(45)]
        public string? Ip { get; set; }

        [MaxLength(500)]
        public string? UserAgent { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ClosedAt { get; set; }

        [MaxLength(255)]
        public string? TokenJtiHash { get; set; }

        // Navigation properties
        [ForeignKey(nameof(UserId))]
        public User User { get; set; } = null!;
    }
}