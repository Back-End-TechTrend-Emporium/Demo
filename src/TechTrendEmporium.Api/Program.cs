using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Data;
using Logica.Interfaces;
using Logica.Repositories;
using Logica.Services;

var builder = WebApplication.CreateBuilder(args);

// EF Core (con reintentos)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql => sql.EnableRetryOnFailure()
    )
);

// Repos & Services
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "TechTrendEmporium.Api", Version = "v1" });
});

var app = builder.Build();

// Migraciones condicionales
if (builder.Configuration.GetValue<bool>("EF:ApplyMigrationsOnStartup"))
{
    using var scope = app.Services.CreateScope();
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        logger.LogInformation("Starting database migration...");
        await context.Database.MigrateAsync();
        logger.LogInformation("Database migrations applied successfully");
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database");

        // En producción, si quieres que caiga la app cuando falle la migración, deja el throw.
        // Si prefieres que arranque igual, comenta la siguiente línea.
        if (app.Environment.IsProduction())
        {
            logger.LogCritical("Application stopped due to migration failure in Production");
            throw;
        }
    }
}

// === Swagger habilitado por app setting también en Producción ===
var swaggerEnabled = builder.Configuration.GetValue<bool>("Swagger:Enabled",
                      app.Environment.IsDevelopment());

if (swaggerEnabled)
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "TechTrendEmporium.Api v1");

        // Si quieres que la raíz (/) abra Swagger en prod, activa Swagger:ServeAtRoot=true
        if (builder.Configuration.GetValue<bool>("Swagger:ServeAtRoot", false))
        {
            c.RoutePrefix = string.Empty; // sirve Swagger en "/"
        }
    });
}

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/health", () => "Healthy");

app.Run();
