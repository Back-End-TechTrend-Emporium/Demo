using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Data;
using External.FakeStore;
using Logica.Interfaces;
using Logica.Repositories;
using Logica.Services;
using External.FakeStore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

// --- CARGAR USER SECRETS EN PRODUCTION PARA TESTING LOCAL  ---
if (builder.Environment.IsProduction())
{
    builder.Configuration.AddUserSecrets<Program>();
    Console.WriteLine("[DEBUG] User Secrets loaded for Production environment");
}

// --- Lógica de conexión a la base de datos robusta  ---
string? connectionString;
if (builder.Environment.IsDevelopment())
{
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    Console.WriteLine($"[DEVELOPMENT] Using local database: {connectionString}");
}
else
{
    connectionString =
        builder.Configuration["ConnectionStrings:ProductionConnection"]
        ?? builder.Configuration.GetConnectionString("ProductionConnection")
        ?? builder.Configuration.GetConnectionString("DefaultConnection")
        ?? Environment.GetEnvironmentVariable("ConnectionStrings__ProductionConnection")
        ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
        ?? Environment.GetEnvironmentVariable("SQLCONNSTR_ProductionConnection")
        ?? Environment.GetEnvironmentVariable("SQLCONNSTR_DefaultConnection")
        ?? Environment.GetEnvironmentVariable("CUSTOMCONNSTR_DefaultConnection");

    Console.WriteLine($"[PRODUCTION] Using Azure database");
    Console.WriteLine($"[DEBUG] Connection string found: {!string.IsNullOrEmpty(connectionString)}");
}

if (string.IsNullOrWhiteSpace(connectionString))
{
    // Lanza un error si no se encuentra la cadena de conexión
    throw new InvalidOperationException(
        "No se encontró la cadena de conexión. " +
        "Define la Connection String apropiada para el entorno actual.");
}

// --- EF Core con política de reintentos ---
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure(
        maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorNumbersToAdd: null)));

// --- HttpClient para FakeStore API  ---
builder.Services.AddHttpClient<IFakeStoreApiClient, FakeStoreApiClient>(client =>
{
    var fakeStoreConfig = builder.Configuration.GetSection("FakeStoreApi");
    var baseUrl = fakeStoreConfig["BaseUrl"] ?? "https://fakestoreapi.com";
    var timeoutSeconds = fakeStoreConfig.GetValue<int>("TimeoutSeconds", 30);

    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
});

// --- Inyección de Dependencias (Combinado) ---
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
// --- Servicios de Autenticación  ---
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// --- Configuración de Autenticación JWT  ---
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

// --- Configuración de Swagger con soporte para JWT  ---
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "TechTrendEmporium.Api", Version = "v1" });
    // Añade la definición de seguridad para Bearer (JWT)
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Autorización JWT usando el esquema Bearer. Ingresa 'Bearer' [espacio] y luego tu token.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// --- Migraciones y creación de la base de datos al iniciar ---
if (builder.Configuration.GetValue<bool>("EF:ApplyMigrationsOnStartup"))
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        logger.LogInformation("Setting up database...");
        if (app.Environment.IsDevelopment())
        {
            logger.LogInformation("Creating/verifying development database...");
            await context.Database.EnsureCreatedAsync();
            logger.LogInformation("Development database setup successfully.");
        }
        else
        {
            logger.LogInformation("Applying database migrations for production...");
            await context.Database.MigrateAsync();
            logger.LogInformation("Database migrations applied successfully.");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while setting up the database.");
        if (app.Environment.IsProduction()) throw; // Detener la app si la DB falla en producción
    }
}

// --- Asegurar la existencia del usuario de sistema  ---
if (builder.Configuration.GetValue<bool>("EnsureSystemUser", true))
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var systemUserId = new Guid("00000000-0000-0000-0000-000000000001");
        if (!await context.Users.AnyAsync(u => u.Id == systemUserId))
        {
            context.Users.Add(new Data.Entities.User
            {
                Id = systemUserId,
                Email = "system@techtrendemporium.com",
                Username = "system",
                PasswordHash = "SYSTEM_ACCOUNT_NOT_FOR_LOGIN",
                Role = Data.Entities.Enums.Role.Admin,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();
            logger.LogInformation("System user created successfully.");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while ensuring system user exists.");
    }
}


// --- Configuración del Middleware HTTP ---

// --- Swagger UI (configurable) ---
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

// --- ¡MUY IMPORTANTE EL ORDEN! ---
app.UseAuthentication(); // 1. Identifica quién es el usuario (lee el token).
app.UseAuthorization();  // 2. Verifica si ese usuario tiene permisos.

app.MapControllers();
app.MapGet("/health", () => "Healthy");

app.Run();
