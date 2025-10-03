using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Data;
using External.FakeStore;
using Logica.Interfaces;
using Logica.Repositories;
using Logica.Services;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

// --- INICIO: LÓGICA DE CONEXIÓN RESTAURADA (VERSIÓN OFICIAL) ---
string? connectionString;

if (builder.Environment.IsDevelopment())
{
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    Console.WriteLine($"[DEVELOPMENT] Using local database.");
}
else
{
    // En producción, usar la conexión de Azure desde múltiples fuentes
    connectionString =
        builder.Configuration["ConnectionStrings:ProductionConnection"] // User Secrets (local testing)
        ?? builder.Configuration.GetConnectionString("ProductionConnection") // User Secrets (local testing)
        ?? builder.Configuration.GetConnectionString("DefaultConnection") // Azure App Service Connection String
        ?? Environment.GetEnvironmentVariable("ConnectionStrings__ProductionConnection")
        ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
        ?? Environment.GetEnvironmentVariable("SQLCONNSTR_ProductionConnection")
        ?? Environment.GetEnvironmentVariable("SQLCONNSTR_DefaultConnection")
        ?? Environment.GetEnvironmentVariable("CUSTOMCONNSTR_DefaultConnection");

    Console.WriteLine($"[PRODUCTION] Using Azure database");
    Console.WriteLine($"[DEBUG] Connection string found: {!string.IsNullOrEmpty(connectionString)}");
    // Debug adicional para Azure App Service

    if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME")))

    {

        Console.WriteLine($"[DEBUG] Running in Azure App Service: {Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME")}");

        Console.WriteLine($"[DEBUG] DefaultConnection available: {!string.IsNullOrEmpty(builder.Configuration.GetConnectionString("DefaultConnection"))}");

    }
}

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("No se encontró la cadena de conexión. Define la Connection String apropiada para el entorno actual.");
}
// --- FIN: LÓGICA DE CONEXIÓN RESTAURADA ---

// --- EF Core con política de reintentos ---
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure(
        maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorNumbersToAdd: null)));

// ... (El resto del archivo es igual al que te di en la respuesta anterior, ya que estaba correcto)

// --- HttpClient para FakeStore API ---
builder.Services.AddHttpClient<IFakeStoreApiService, FakeStoreApiService>(client =>
{
    var fakeStoreConfig = builder.Configuration.GetSection("FakeStoreApi");
    var baseUrl = fakeStoreConfig["BaseUrl"] ?? "https://fakestoreapi.com";
    client.BaseAddress = new Uri(baseUrl);
});

// --- Inyección de Dependencias (completa) ---
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// --- Configuración de Autenticación JWT ---
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!)),
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });
builder.Services.AddAuthorization();

// --- Servicios estándar de la API ---
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// --- Configuración de Swagger con soporte para JWT ---
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "TechTrendEmporium.Api", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme { /* ... */ });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement { /* ... */ });
});

var app = builder.Build();

// ========================================================================
// === TAREAS DE INICIO: MIGRACIONES Y SEEDERS DE BASE DE DATOS ===
// ========================================================================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        var context = services.GetRequiredService<AppDbContext>();

        logger.LogInformation("Applying database migrations...");
        await context.Database.MigrateAsync();
        logger.LogInformation("Database migrations applied successfully.");

        await DbSeeder.SeedUsersAsync(context, logger);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred during database initialization.");
        if (app.Environment.IsProduction()) throw;
    }
}
// ========================================================================

// --- Configuración del Pipeline de HTTP ---
var swaggerEnabled = configuration.GetValue<bool>("Swagger:Enabled", app.Environment.IsDevelopment());
if (swaggerEnabled)
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "TechTrendEmporium.Api v1");
    });
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/health", () => "Healthy");

app.Run();