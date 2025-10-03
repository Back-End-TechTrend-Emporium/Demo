using Data.Entities;
using Data.Entities.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging; // Añadir este using

namespace Data;

public static class DbSeeder
{
    // Un solo método para crear todos los usuarios necesarios
    public static async Task SeedUsersAsync(AppDbContext context, ILogger logger)
    {
        // 1. Crear usuario de Sistema (de la versión oficial)
        var systemUserId = new Guid("00000000-0000-0000-0000-000000000001");
        if (!await context.Users.AnyAsync(u => u.Id == systemUserId))
        {
            context.Users.Add(new User
            {
                Id = systemUserId,
                Name = "System",
                Email = "system@techtrendemporium.com",
                Username = "system",
                PasswordHash = "SYSTEM_ACCOUNT_NOT_FOR_LOGIN", // No se puede usar para login
                Role = Role.Admin,
                IsActive = true
            });
            logger.LogInformation("System user created.");
        }

        // 2. Crear usuario Administrador para pruebas (tu funcionalidad)
        if (!await context.Users.AnyAsync(u => u.Username == "admin"))
        {
            context.Users.Add(new User
            {
                Name = "Admin User",
                Email = "admin@example.com",
                Username = "admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
                Role = Role.Admin,
                IsActive = true
            });
            logger.LogInformation("Admin test user created.");
        }

        // Guardar todos los cambios a la vez
        await context.SaveChangesAsync();
    }
}