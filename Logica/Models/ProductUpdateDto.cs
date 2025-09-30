using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logica.Models
{
    public class ProductUpdateDto
    {
        public string? Title { get; set; }
        
        public decimal? Price { get; set; }
        
        public string? Description { get; set; }
        
        public string? Category { get; set; }
        
        public string? Image { get; set; }
    }
}