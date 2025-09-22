using System.ComponentModel.DataAnnotations;

namespace TechTrendEmporium.Core.Entities;

public enum OrderStatus
{
    Pending = 1,
    Processing = 2,
    Shipped = 3,
    Delivered = 4,
    Cancelled = 5
}

public class Order
{
    public int Id { get; set; }
    
    public string UserId { get; set; } = string.Empty;
    
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    
    public decimal SubTotal { get; set; }
    
    public decimal DiscountAmount { get; set; } = 0;
    
    public decimal TotalAmount { get; set; }
    
    public string? CouponCode { get; set; }
    
    [MaxLength(500)]
    public string ShippingAddress { get; set; } = string.Empty;
    
    // Navigation properties
    public virtual User User { get; set; } = null!;
    public virtual ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}

public class OrderItem
{
    public int Id { get; set; }
    
    public int OrderId { get; set; }
    
    public int ProductId { get; set; }
    
    public int Quantity { get; set; }
    
    public decimal UnitPrice { get; set; }
    
    public decimal TotalPrice { get; set; }
    
    // Navigation properties
    public virtual Order Order { get; set; } = null!;
    public virtual Product Product { get; set; } = null!;
}