using Data.Entities;
using Data.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // DbSets
        public DbSet<User> Users { get; set; }
        public DbSet<Session> Sessions { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Wishlist> Wishlists { get; set; }
        public DbSet<WishlistItem> WishlistItems { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<Coupon> Coupons { get; set; }
        public DbSet<ApprovalJob> ApprovalJobs { get; set; }
        public DbSet<ExternalImportJob> ExternalImportJobs { get; set; }
        public DbSet<ExternalMapping> ExternalMappings { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure User relationships to avoid cascade delete conflicts
            modelBuilder.Entity<User>()
                .HasMany(u => u.CreatedProducts)
                .WithOne(p => p.Creator)
                .HasForeignKey(p => p.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<User>()
                .HasMany(u => u.ApprovedProducts)
                .WithOne(p => p.Approver)
                .HasForeignKey(p => p.ApprovedBy)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<User>()
                .HasMany(u => u.CreatedCategories)
                .WithOne(c => c.Creator)
                .HasForeignKey(c => c.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<User>()
                .HasMany(u => u.ApprovedCategories)
                .WithOne(c => c.Approver)
                .HasForeignKey(c => c.ApprovedBy)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<User>()
                .HasMany(u => u.RequestedJobs)
                .WithOne(j => j.Requester)
                .HasForeignKey(j => j.RequestedBy)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<User>()
                .HasMany(u => u.ReviewedJobs)
                .WithOne(j => j.Reviewer)
                .HasForeignKey(j => j.ReviewedBy)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure unique constraints
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<Category>()
                .HasIndex(c => c.Name)
                .IsUnique();

            modelBuilder.Entity<Category>()
                .HasIndex(c => c.Slug)
                .IsUnique();

            modelBuilder.Entity<Wishlist>()
                .HasIndex(w => w.UserId)
                .IsUnique();

            modelBuilder.Entity<WishlistItem>()
                .HasIndex(wi => new { wi.WishlistId, wi.ProductId })
                .IsUnique();

            modelBuilder.Entity<CartItem>()
                .HasIndex(ci => new { ci.CartId, ci.ProductId })
                .IsUnique();

            modelBuilder.Entity<Coupon>()
                .HasIndex(c => c.Code)
                .IsUnique();

            modelBuilder.Entity<ExternalMapping>()
                .HasIndex(em => new { em.Source, em.SourceType, em.SourceId })
                .IsUnique();

            // Configure performance indexes
            modelBuilder.Entity<Cart>()
                .HasIndex(c => new { c.UserId, c.Status });

            modelBuilder.Entity<Product>()
                .HasIndex(p => p.Title);

            modelBuilder.Entity<Product>()
                .HasIndex(p => p.Price);

            modelBuilder.Entity<Product>()
                .HasIndex(p => new { p.CategoryId, p.State });

            modelBuilder.Entity<Review>()
                .HasIndex(r => new { r.ProductId, r.CreatedAt });

            modelBuilder.Entity<ApprovalJob>()
                .HasIndex(aj => new { aj.Status, aj.CreatedAt });

            modelBuilder.Entity<ApprovalJob>()
                .HasIndex(aj => new { aj.Type, aj.Status });

            // Configure decimal precision
            modelBuilder.Entity<Product>()
                .Property(p => p.Price)
                .HasPrecision(10, 2);

            modelBuilder.Entity<Product>()
                .Property(p => p.RatingAverage)
                .HasPrecision(2, 1);

            modelBuilder.Entity<Cart>()
                .Property(c => c.TotalBeforeDiscount)
                .HasPrecision(10, 2);

            modelBuilder.Entity<Cart>()
                .Property(c => c.DiscountAmount)
                .HasPrecision(10, 2);

            modelBuilder.Entity<Cart>()
                .Property(c => c.ShippingCost)
                .HasPrecision(10, 2);

            modelBuilder.Entity<Cart>()
                .Property(c => c.FinalTotal)
                .HasPrecision(10, 2);

            modelBuilder.Entity<CartItem>()
                .Property(ci => ci.UnitPriceSnapshot)
                .HasPrecision(10, 2);

            modelBuilder.Entity<Coupon>()
                .Property(c => c.DiscountPercentage)
                .HasPrecision(5, 2);

            // Configure check constraints for business rules
            modelBuilder.Entity<Product>()
                .HasCheckConstraint("CK_Product_InventoryAvailable", "[InventoryAvailable] >= 0");

            modelBuilder.Entity<Product>()
                .HasCheckConstraint("CK_Product_InventoryTotal", "[InventoryTotal] >= 0");

            modelBuilder.Entity<Product>()
                .HasCheckConstraint("CK_Product_InventoryAvailable_LTE_Total", "[InventoryAvailable] <= [InventoryTotal]");

            modelBuilder.Entity<Review>()
                .HasCheckConstraint("CK_Review_Rating", "[Rating] >= 1 AND [Rating] <= 5");

            modelBuilder.Entity<Product>()
                .HasCheckConstraint("CK_Product_RatingAverage", "[RatingAverage] >= 0.0 AND [RatingAverage] <= 5.0");

            modelBuilder.Entity<Coupon>()
                .HasCheckConstraint("CK_Coupon_DiscountPercentage", "[DiscountPercentage] >= 0 AND [DiscountPercentage] <= 100");

            modelBuilder.Entity<CartItem>()
                .HasCheckConstraint("CK_CartItem_Quantity", "[Quantity] >= 1");

            // Configure enum conversions
            modelBuilder.Entity<User>()
                .Property(u => u.Role)
                .HasConversion<string>();

            modelBuilder.Entity<Session>()
                .Property(s => s.Status)
                .HasConversion<string>();

            modelBuilder.Entity<Category>()
                .Property(c => c.State)
                .HasConversion<string>();

            modelBuilder.Entity<Product>()
                .Property(p => p.State)
                .HasConversion<string>();

            modelBuilder.Entity<Cart>()
                .Property(c => c.Status)
                .HasConversion<string>();

            modelBuilder.Entity<ApprovalJob>()
                .Property(aj => aj.Type)
                .HasConversion<string>();

            modelBuilder.Entity<ApprovalJob>()
                .Property(aj => aj.Operation)
                .HasConversion<string>();

            modelBuilder.Entity<ApprovalJob>()
                .Property(aj => aj.Status)
                .HasConversion<string>();

            modelBuilder.Entity<ExternalImportJob>()
                .Property(eij => eij.Source)
                .HasConversion<string>();

            modelBuilder.Entity<ExternalMapping>()
                .Property(em => em.Source)
                .HasConversion<string>();

            modelBuilder.Entity<Notification>()
                .Property(n => n.Status)
                .HasConversion<string>();
        }
    }
}
