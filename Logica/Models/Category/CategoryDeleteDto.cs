using System.ComponentModel.DataAnnotations;

namespace Logica.Models.Category
{
    public class CategoryDeleteDto
    {
        [Required]
        public Guid Id { get; set; }
    }
}