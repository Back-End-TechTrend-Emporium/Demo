using System.ComponentModel.DataAnnotations;
using Data.Entities.Enums;

namespace Logica.Models.Carts
{
    public class CheckoutRequest
    {
        [Required]
        [MaxLength(512)]
        public string Address { get; set; } = string.Empty;

        [Required]
        public PaymentMethod PaymentMethod { get; set; }
    }
}
