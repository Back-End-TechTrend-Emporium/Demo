using System.ComponentModel.DataAnnotations;
using Data.Entities.Enums;

namespace Data.Entities
{
    public class Notification
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(20)]
        public string Type { get; set; } = string.Empty; // 'EMAIL' | 'WEBHOOK' | 'SLACK' | 'INTERNAL'

        [MaxLength(255)]
        public string? Target { get; set; }

        public string? PayloadJson { get; set; }

        [Required]
        public NotificationStatus Status { get; set; } = NotificationStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? SentAt { get; set; }

        public string? ErrorMessage { get; set; }
    }
}