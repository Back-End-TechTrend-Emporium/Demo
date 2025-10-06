using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Logica.Models;

namespace Logica.Models.Products
{
    public class ProductUpdateDto
    {
        public string? Title { get; set; }
        
        public decimal? Price { get; set; }
        
        public string? Description { get; set; }
        
        public string? Category { get; set; }
        
        public string? Image { get; set; }
        
        public RatingDto? Rating { get; set; }
        
        public InventoryDto? Inventory { get; set; }
    }
}