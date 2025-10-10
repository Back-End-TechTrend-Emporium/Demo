using Data.Entities;
using Data.Entities.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging; 

namespace Data;

public static class DbSeeder
{
    // This method creates all users needed for testing
    public static async Task SeedUsersAsync(AppDbContext context, ILogger logger)
    {
        // 1. Create system user if not exists
        var systemUserId = new Guid("00000000-0000-0000-0000-000000000001");
        if (!await context.Users.AnyAsync(u => u.Username == "superadmin" || u.Email == "admin@tte.com"))
        {
            context.Users.Add(new User
            {
                Name = "Admin super",
                Email = "admin@tte.com",
                Username = "superadmin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
                Role = Role.SuperAdmin,
                IsActive = true
            });
            logger.LogInformation("SuperAdmin test user created.");
        }
        if (!await context.Users.AnyAsync(u => u.Username == "Shopper one" || u.Email == "shopper@tte.com"))
        {
            context.Users.Add(new User
            {
                Name = "Shopper User",
                Email = "shopper@tte.com",
                Username = "Shopper one",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
                Role = Role.Shopper,
                IsActive = true
            });
            logger.LogInformation("Shopper user test created");
        }

        // Guardar todos los cambios a la vez
        await context.SaveChangesAsync();
    }
}