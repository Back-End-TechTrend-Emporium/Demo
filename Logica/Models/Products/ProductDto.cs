using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logica.Models.Products
{
    public class ProductDto
    {
        public Guid Id { get; set; } // Cambiar de int a Guid
        
        public string Title { get; set; } = string.Empty;
        
        public decimal Price { get; set; }
        
        public string Description { get; set; } = string.Empty;
        
        public string Category { get; set; } = string.Empty;
        
        public string Image { get; set; } = string.Empty;
        
        public RatingDto? Rating { get; set; }
    }
}