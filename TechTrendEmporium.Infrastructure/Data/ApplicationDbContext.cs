using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TechTrendEmporium.Core.Entities;

namespace TechTrendEmporium.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<User>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Product> Products { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Cart> Carts { get; set; }
    public DbSet<CartItem> CartItems { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<Review> Reviews { get; set; }
    public DbSet<WishlistItem> WishlistItems { get; set; }
    public DbSet<Coupon> Coupons { get; set; }
    public DbSet<UserSession> UserSessions { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Product configuration
        builder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).IsRequired();
            entity.Property(e => e.Price).HasPrecision(18, 2);
            entity.HasIndex(e => e.ExternalId).IsUnique();
            
            entity.HasOne(e => e.Category)
                .WithMany(e => e.Products)
                .HasForeignKey(e => e.CategoryId);
        });

        // Category configuration
        builder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => e.ExternalId).IsUnique();
        });

        // Cart configuration
        builder.Entity<Cart>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User)
                .WithMany(e => e.Carts)
                .HasForeignKey(e => e.UserId);
        });

        // CartItem configuration
        builder.Entity<CartItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UnitPrice).HasPrecision(18, 2);
            
            entity.HasOne(e => e.Cart)
                .WithMany(e => e.Items)
                .HasForeignKey(e => e.CartId);
                
            entity.HasOne(e => e.Product)
                .WithMany(e => e.CartItems)
                .HasForeignKey(e => e.ProductId);
        });

        // Order configuration
        builder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SubTotal).HasPrecision(18, 2);
            entity.Property(e => e.DiscountAmount).HasPrecision(18, 2);
            entity.Property(e => e.TotalAmount).HasPrecision(18, 2);
            entity.Property(e => e.ShippingAddress).IsRequired().HasMaxLength(500);
            
            entity.HasOne(e => e.User)
                .WithMany(e => e.Orders)
                .HasForeignKey(e => e.UserId);
        });

        // OrderItem configuration
        builder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UnitPrice).HasPrecision(18, 2);
            entity.Property(e => e.TotalPrice).HasPrecision(18, 2);
            
            entity.HasOne(e => e.Order)
                .WithMany(e => e.Items)
                .HasForeignKey(e => e.OrderId);
                
            entity.HasOne(e => e.Product)
                .WithMany(e => e.OrderItems)
                .HasForeignKey(e => e.ProductId);
        });

        // Review configuration
        builder.Entity<Review>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Comment).HasMaxLength(1000);
            
            entity.HasOne(e => e.User)
                .WithMany(e => e.Reviews)
                .HasForeignKey(e => e.UserId);
                
            entity.HasOne(e => e.Product)
                .WithMany(e => e.Reviews)
                .HasForeignKey(e => e.ProductId);
        });

        // WishlistItem configuration
        builder.Entity<WishlistItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasOne(e => e.User)
                .WithMany(e => e.WishlistItems)
                .HasForeignKey(e => e.UserId);
                
            entity.HasOne(e => e.Product)
                .WithMany(e => e.WishlistItems)
                .HasForeignKey(e => e.ProductId);
                
            entity.HasIndex(e => new { e.UserId, e.ProductId }).IsUnique();
        });

        // Coupon configuration
        builder.Entity<Coupon>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(200);
            entity.Property(e => e.DiscountPercentage).HasPrecision(5, 2);
            entity.Property(e => e.MaxDiscountAmount).HasPrecision(18, 2);
            entity.Property(e => e.MinimumOrderAmount).HasPrecision(18, 2);
            entity.HasIndex(e => e.Code).IsUnique();
        });

        // UserSession configuration
        builder.Entity<UserSession>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SessionId).IsRequired();
            
            entity.HasOne(e => e.User)
                .WithMany(e => e.Sessions)
                .HasForeignKey(e => e.UserId);
        });

        // Seed data
        SeedData(builder);
    }

    private static void SeedData(ModelBuilder builder)
    {
        // Seed categories
        builder.Entity<Category>().HasData(
            new Category { Id = 1, Name = "Electronics", Description = "Electronic devices and gadgets", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Category { Id = 2, Name = "Jewelery", Description = "Jewelry and accessories", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Category { Id = 3, Name = "Men's Clothing", Description = "Men's fashion and clothing", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Category { Id = 4, Name = "Women's Clothing", Description = "Women's fashion and clothing", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        );

        // Seed coupons
        builder.Entity<Coupon>().HasData(
            new Coupon 
            { 
                Id = 1, 
                Code = "WELCOME10", 
                Description = "Welcome discount - 10% off", 
                DiscountPercentage = 10, 
                MaxDiscountAmount = 50, 
                MinimumOrderAmount = 100,
                ValidFrom = DateTime.UtcNow,
                ValidTo = DateTime.UtcNow.AddYears(1),
                UsageLimit = 1000,
                CreatedAt = DateTime.UtcNow 
            },
            new Coupon 
            { 
                Id = 2, 
                Code = "SAVE15", 
                Description = "Save 15% on orders over $200", 
                DiscountPercentage = 15, 
                MaxDiscountAmount = 100, 
                MinimumOrderAmount = 200,
                ValidFrom = DateTime.UtcNow,
                ValidTo = DateTime.UtcNow.AddYears(1),
                UsageLimit = 500,
                CreatedAt = DateTime.UtcNow 
            }
        );
    }
}