using Data.Entities;
using Data.Entities.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection; 
using Microsoft.AspNetCore.Builder;
using BCrypt.Net;

namespace Data;

public static class DbSeeder
{
    // Este será un método de extensión para facilitar su llamada desde Program.cs
    public static async Task SeedDatabaseAsync(this IApplicationBuilder app)
    {
        // Creamos un "scope" para obtener los servicios, como el DbContext
        using var scope = app.ApplicationServices.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // 1. Asegura que la base de datos esté creada y las migraciones aplicadas
        await context.Database.MigrateAsync();

        // 2. Busca si ya existe un usuario Administrador o SuperAdmin
        var adminExists = await context.Users.AnyAsync(u => u.Role == Role.Admin || u.Role == Role.SuperAdmin);

        // 3. Si no existe, créalo
        if (!adminExists)
        {
            var adminUser = new User
            {
                Name = "Admin User",
                Email = "admin@example.com",
                Username = "admin",
                // Hasheamos la contraseña para que el login funcione
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
                Role = Role.SuperAdmin,
                IsActive = true
            };
            context.Users.Add(adminUser);
            await context.SaveChangesAsync();
        }
    }
}