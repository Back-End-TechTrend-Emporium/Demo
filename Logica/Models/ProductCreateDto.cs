using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logica.Models
{
    public class ProductCreateDto
    {
        public required string Title { get; set; }
        
        public decimal Price { get; set; }
        
        public required string Description { get; set; }
        
        public required string Category { get; set; }
        
        public required string Image { get; set; }
    }
}