using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logica.Models
{
    public class CategoryUpdateDto
    {
        [MaxLength(100)]
        public string? Name { get; set; }
        
        [MaxLength(120)]
        public string? Slug { get; set; }
    }
}