using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logica.Models
{
    public class CartItemDto
    {
        public Guid ProductId { get; set; }
        
        [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor a 0")]
        public int Quantity { get; set; }
        
        // Propiedades adicionales útiles para el frontend
        public string? ProductTitle { get; set; }
        public decimal? UnitPrice { get; set; }
        public string? ProductImage { get; set; }
        public decimal? TotalPrice { get; set; }
    }
}