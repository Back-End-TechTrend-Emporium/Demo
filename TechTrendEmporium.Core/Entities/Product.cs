using System.ComponentModel.DataAnnotations;

namespace TechTrendEmporium.Core.Entities;

public class Product
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    public string Description { get; set; } = string.Empty;
    
    [Required]
    public decimal Price { get; set; }
    
    public string? ImageUrl { get; set; }
    
    public int CategoryId { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public int StockQuantity { get; set; } = 0;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // FakeStore API integration
    public int? ExternalId { get; set; }
    
    public decimal? ExternalRating { get; set; }
    
    public int? ExternalRatingCount { get; set; }
    
    // Navigation properties
    public virtual Category Category { get; set; } = null!;
    public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
    public virtual ICollection<WishlistItem> WishlistItems { get; set; } = new List<WishlistItem>();
}