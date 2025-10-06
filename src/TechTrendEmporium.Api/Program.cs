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

// === CARGAR USER SECRETS EN PRODUCTION PARA TESTING LOCAL ===
if (builder.Environment.IsProduction())
{
    builder.Configuration.AddUserSecrets<Program>();
    Console.WriteLine("[DEBUG] User Secrets loaded for Production environment");
}

// === Resolver connection string según el entorno ===
string? connectionString;

if (builder.Environment.IsDevelopment())
{
    // En desarrollo, usar la conexión local del appsettings.Development.json
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    Console.WriteLine($"[DEVELOPMENT] Using local database: {connectionString}");
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
    // Agregar debugging para ver qué configuración está disponible
    Console.WriteLine("[DEBUG] Available configuration keys:");
    foreach (var item in builder.Configuration.AsEnumerable())
    {
        if (item.Key.Contains("Connection", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine($"  {item.Key} = {(item.Value?.Length > 0 ? "[SET]" : "[EMPTY]")}");
        }
    }
    
    throw new InvalidOperationException(
        "No se encontró la cadena de conexión. " +
        "Define la Connection String apropiada para el entorno actual.");
}

// === EF Core (con reintentos) ===
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        connectionString,
        sql => sql.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null)));

// === HttpClient para FakeStore API ===
builder.Services.AddHttpClient<IFakeStoreApiService, FakeStoreApiService>(client =>
{
    var fakeStoreConfig = builder.Configuration.GetSection("FakeStoreApi");
    var baseUrl = fakeStoreConfig["BaseUrl"] ?? "https://fakestoreapi.com";
    var timeoutSeconds = fakeStoreConfig.GetValue<int>("TimeoutSeconds", 30);
    
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
});

// === Dependency Injection ===
// Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IReviewRepository, ReviewRepository>();

// Services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IReviewService, ReviewService>();

// Authentication Services
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// === Configuración de Autenticación JWT ===
// Obtener la clave JWT de múltiples ubicaciones para compatibilidad con Azure
var jwtKey = configuration["Jwt:Key"] 
          ?? configuration["Jwt_Key"] 
          ?? Environment.GetEnvironmentVariable("Jwt_Key")
          ?? Environment.GetEnvironmentVariable("Jwt__Key");

//Debug ayudado con la IA
if (string.IsNullOrWhiteSpace(jwtKey))
{
    Console.WriteLine("[ERROR] JWT Key not found in any configuration source");
    Console.WriteLine("[DEBUG] Available JWT-related configuration:");
    foreach (var item in configuration.AsEnumerable())
    {
        if (item.Key.Contains("Jwt", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine($"  {item.Key} = {(item.Value?.Length > 0 ? "[SET]" : "[EMPTY]")}");
        }
    }
    throw new InvalidOperationException("La clave JWT no fue encontrada en ninguna ubicación válida.");
}

Console.WriteLine($"[DEBUG] JWT Key found: {jwtKey.Length} characters");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// === Servicios estándar de la API ===
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// === Configuración de Swagger con soporte para JWT ===
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "TechTrendEmporium.Api", Version = "v1" });
    c.EnableAnnotations();

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

// === Crear/verificar base de datos ===
if (builder.Configuration.GetValue<bool>("EF:ApplyMigrationsOnStartup"))
{
    using var scope = app.Services.CreateScope();
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        logger.LogInformation("Setting up database...");
        
        // Para desarrollo, usar EnsureCreated es más simple
        if (builder.Environment.IsDevelopment())
        {
            logger.LogInformation("Creating/verifying development database...");
            await context.Database.EnsureCreatedAsync();
            logger.LogInformation("Development database created/verified successfully");
        }
        else
        {
            // En producción, usar migraciones
            logger.LogInformation("Applying database migrations...");
            await context.Database.MigrateAsync();
            logger.LogInformation("Database migrations applied successfully");
        }
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while setting up the database");

        if (app.Environment.IsProduction())
        {
            logger.LogCritical("Application stopped due to database setup failure in Production");
            throw;
        }
        else
        {
            logger.LogWarning("Database setup failed in Development. The application will continue but may not function correctly.");
        }
    }
}

// === Ensure system user exists ===
if (builder.Configuration.GetValue<bool>("EnsureSystemUser", true))
{
    using var scope = app.Services.CreateScope();
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        
        var systemUserId = new Guid("00000000-0000-0000-0000-000000000001");
        var systemUser = await context.Users.FindAsync(systemUserId);
        
        if (systemUser == null)
        {
            systemUser = new Data.Entities.User
            {
                Id = systemUserId,
                Email = "system@techtrendemporium.com",
                Username = "system",
                PasswordHash = "SYSTEM_ACCOUNT_NOT_FOR_LOGIN",
                Role = Data.Entities.Enums.Role.Admin,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            
            context.Users.Add(systemUser);
            await context.SaveChangesAsync();
            logger.LogInformation("System user created successfully");
        }
        else
        {
            logger.LogInformation("System user already exists");
        }
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while ensuring system user exists");
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

// === ¡MUY IMPORTANTE EL ORDEN! ===
app.UseAuthentication(); // 1. Identifica quién es el usuario (lee el token).
app.UseAuthorization();  // 2. Verifica si ese usuario tiene permisos.

app.MapControllers();
app.MapGet("/health", () => "Healthy");

app.Run();