using System.ComponentModel.DataAnnotations;

namespace Logica.Models.Wishlist
{
    public class WishlistRemoveProductDto
    {
        [Required] 
        public Guid ProductId { get; set; }
    }
}