using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Data;
using Logica.Interfaces;
using Logica.Repositories;
using Logica.Services;

var builder = WebApplication.CreateBuilder(args);


// === Resolver connection string (config -> env vars de Azure App Service) ===
string? connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")          // si el pipeline la setea así
    ?? Environment.GetEnvironmentVariable("SQLCONNSTR_DefaultConnection")                 // App Service (type=SQLAzure)
    ?? Environment.GetEnvironmentVariable("CUSTOMCONNSTR_DefaultConnection");             // App Service (custom)

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "No se encontró la cadena de conexión 'DefaultConnection'. " +
        "Define la Connection String en Azure App Service (Configuration > Connection strings) " +
        "o inyecta la variable desde el pipeline.");
}

// === EF Core (con reintentos) ===
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        connectionString,
        sql => sql.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null)));

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

// === Migraciones condicionales ===
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

        // En producción, si quieres que la app caiga al fallar la migración, deja el throw.
        if (app.Environment.IsProduction())
        {
            logger.LogCritical("Application stopped due to migration failure in Production");
            throw;
        }
    }
}

// === Swagger (habilitable en Prod con Swagger:Enabled y Swagger:ServeAtRoot) ===
var swaggerEnabled = builder.Configuration.GetValue<bool>("Swagger:Enabled",
                      app.Environment.IsDevelopment());

if (swaggerEnabled)
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "TechTrendEmporium.Api v1");
        if (builder.Configuration.GetValue<bool>("Swagger:ServeAtRoot", false))
            c.RoutePrefix = string.Empty; // sirve Swagger en "/"
    });
}

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/health", () => "Healthy");

app.Run();
