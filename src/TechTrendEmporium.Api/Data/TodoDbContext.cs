using Back_End_TechTrend_Emporium.Models;
using Microsoft.EntityFrameworkCore;

namespace Back_End_TechTrend_Emporium.Data
{
    public class TodoDbContext : DbContext
    {
        public DbSet<TodoItem> Todos => Set<TodoItem>();

        public TodoDbContext(DbContextOptions<TodoDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TodoItem>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Title).IsRequired().HasMaxLength(200);
                b.Property(x => x.IsCompleted).IsRequired();
                b.Property(x => x.CreatedAt).IsRequired();
            });
        }
    }
}