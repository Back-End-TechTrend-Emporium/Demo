using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Back_End_TechTrend_Emporium.Data
{
    public sealed class DbContextFactory : IDesignTimeDbContextFactory<TodoDbContext>
    {
        public TodoDbContext CreateDbContext(string[] args)
        {
            var options = new DbContextOptionsBuilder<TodoDbContext>()
                .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=CompanyTodo;Integrated Security=true;TrustServerCertificate=true")
                .Options;
            return new TodoDbContext(options);
        }
    }
}