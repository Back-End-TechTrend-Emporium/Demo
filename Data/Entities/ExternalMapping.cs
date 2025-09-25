using System.ComponentModel.DataAnnotations;
using Data.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace Data.Entities
{
    [Index(nameof(Source), nameof(SourceType), nameof(SourceId), IsUnique = true)]
    [Index(nameof(SourceId))]
    public class ExternalMapping
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public ExternalSource Source { get; set; } = ExternalSource.FakeStore;

        [Required]
        [MaxLength(20)]
        public string SourceType { get; set; } = string.Empty; // 'PRODUCT' | 'CATEGORY'

        [Required]
        [MaxLength(100)]
        public string SourceId { get; set; } = string.Empty;

        [Required]
        public Guid InternalId { get; set; }

        public string? SnapshotJson { get; set; }

        public DateTime ImportedAt { get; set; } = DateTime.UtcNow;
    }
}