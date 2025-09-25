using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Data.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace Data.Entities
{
    [Index(nameof(Status), nameof(CreatedAt))]
    [Index(nameof(Type), nameof(Status))]
    public class ApprovalJob
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public JobType Type { get; set; }

        [Required]
        public JobOperation Operation { get; set; }

        [Required]
        public Guid EntityId { get; set; }

        [Required]
        public Guid RequestedBy { get; set; }

        [Required]
        public JobStatus Status { get; set; } = JobStatus.Pending;

        public Guid? ReviewedBy { get; set; }

        public string? ReviewComment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? DecidedAt { get; set; }

        // Navigation properties
        [ForeignKey(nameof(RequestedBy))]
        public User Requester { get; set; } = null!;

        [ForeignKey(nameof(ReviewedBy))]
        public User? Reviewer { get; set; }
    }
}