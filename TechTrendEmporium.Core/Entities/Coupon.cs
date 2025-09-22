using System.ComponentModel.DataAnnotations;

namespace TechTrendEmporium.Core.Entities;

public class Coupon
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;
    
    [MaxLength(200)]
    public string Description { get; set; } = string.Empty;
    
    public decimal DiscountPercentage { get; set; }
    
    public decimal? MaxDiscountAmount { get; set; }
    
    public decimal? MinimumOrderAmount { get; set; }
    
    public DateTime ValidFrom { get; set; } = DateTime.UtcNow;
    
    public DateTime ValidTo { get; set; }
    
    public int UsageLimit { get; set; } = 1;
    
    public int UsedCount { get; set; } = 0;
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class UserSession
{
    public int Id { get; set; }
    
    public string UserId { get; set; } = string.Empty;
    
    public string SessionId { get; set; } = string.Empty;
    
    public DateTime LoginTime { get; set; } = DateTime.UtcNow;
    
    public DateTime? LogoutTime { get; set; }
    
    public string? IpAddress { get; set; }
    
    public string? UserAgent { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    // Navigation properties
    public virtual User User { get; set; } = null!;
}