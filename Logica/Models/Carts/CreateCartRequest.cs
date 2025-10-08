using Data.Entities.Enums;

namespace Logica.Models.Carts
{
    public class CreateCartRequest
    {
        public Guid UserId { get; set; }
        public CartStatus Status { get; set; } = CartStatus.Active;
        public List<CreateCartItemRequest> Items { get; set; } = new List<CreateCartItemRequest>();
        public Guid? CouponId { get; set; }
    }

    public class CreateCartItemRequest
    {
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
    }
}