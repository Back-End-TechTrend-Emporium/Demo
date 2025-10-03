using Logica.Models.Category;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logica.Models
{
    public class CartDto
    {
        public Guid Id { get; set; }
        
        public Guid UserId { get; set; }
        
        public string Status { get; set; } = string.Empty;
        
        public decimal TotalBeforeDiscount { get; set; }
        
        public decimal DiscountAmount { get; set; }
        
        public decimal ShippingCost { get; set; }
        
        public decimal FinalTotal { get; set; }
        
        public DateTime CreatedAt { get; set; }
        
        public required IReadOnlyList<CartItemDto> Products { get; set; }
        
        public string? CouponCode { get; set; }
    }
}