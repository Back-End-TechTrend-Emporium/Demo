using System.ComponentModel.DataAnnotations;

namespace TechTrendEmporium.Core.Entities;

public class Review
{
    public int Id { get; set; }
    
    public string UserId { get; set; } = string.Empty;
    
    public int ProductId { get; set; }
    
    [Range(1, 5)]
    public int Rating { get; set; }
    
    [MaxLength(1000)]
    public string Comment { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public bool IsApproved { get; set; } = false;
    
    // Navigation properties
    public virtual User User { get; set; } = null!;
    public virtual Product Product { get; set; } = null!;
}

public class WishlistItem
{
    public int Id { get; set; }
    
    public string UserId { get; set; } = string.Empty;
    
    public int ProductId { get; set; }
    
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual User User { get; set; } = null!;
    public virtual Product Product { get; set; } = null!;
}