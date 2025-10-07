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
        
        public string UserId { get; set; } = string.Empty;
        
        public List<int> ShoppingCart { get; set; } = new();
        
        public CouponAppliedDto? CouponApplied { get; set; }
        
        public decimal TotalBeforeDiscount { get; set; }
        
        public decimal TotalAfterDiscount { get; set; }
        
        public decimal ShippingCost { get; set; }
        
        public decimal FinalTotal { get; set; }
    }

    public class CouponAppliedDto
    {
        public string CouponCode { get; set; } = string.Empty;
        
        public decimal DiscountPercentage { get; set; }
    }
}