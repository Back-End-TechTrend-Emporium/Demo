using System.ComponentModel.DataAnnotations;

namespace Logica.Models.Wishlist
{
    public class WishlistAddProductDto
    {
        [Required]
        public Guid ProductId { get; set; }
    }
}