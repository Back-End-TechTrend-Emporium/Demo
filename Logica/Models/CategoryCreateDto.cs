using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logica.Models
{
    public class CategoryCreateDto
    {
        [Required]
        [MaxLength(100)]
        public required string Name { get; set; }
        
        [Required]
        [MaxLength(120)]
        public required string Slug { get; set; }
    }
}