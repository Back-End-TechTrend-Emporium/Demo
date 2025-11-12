using Data.Entities.Enums;

namespace Logica.Models.Carts
{
    public class UpdateCartRequest
    {
        public CartStatus? Status { get; set; }
        public List<UpdateCartItemRequest> Items { get; set; } = new List<UpdateCartItemRequest>();
        public Guid? CouponId { get; set; }
    }

    public class UpdateCartItemRequest
    {
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
    }
}