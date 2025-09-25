using System.ComponentModel.DataAnnotations;
using Data.Entities.Enums;

namespace Data.Entities
{
    public class ExternalImportJob
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public ExternalSource Source { get; set; } = ExternalSource.FakeStore;

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "PENDING";

        public DateTime? StartedAt { get; set; }

        public DateTime? FinishedAt { get; set; }

        public int ProductsImported { get; set; } = 0;

        public int CategoriesImported { get; set; } = 0;

        public string? ErrorMessage { get; set; }
    }
}