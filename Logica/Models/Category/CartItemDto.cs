using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logica.Models.Category
{
    public class CartItemDto
    {
        public Guid ProductId { get; set; }
        
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
        public int Quantity { get; set; }
        
        // Additional properties useful for frontend
        public string? ProductTitle { get; set; }
        public decimal? UnitPrice { get; set; }
        public string? ProductImage { get; set; }
        public decimal? TotalPrice { get; set; }
    }
}