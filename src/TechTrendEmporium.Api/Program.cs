using Data;
using External.FakeStore;
using Logica.Interfaces;
using Logica.Repositories;
using Logica.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

if (builder.Environment.IsProduction())
{
    builder.Configuration.AddUserSecrets<Program>();
    Console.WriteLine("[DEBUG] User Secrets loaded for Production environment");
}

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
    if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME")))
    {
        Console.WriteLine($"[DEBUG] Running in Azure App Service: {Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME")}");
        Console.WriteLine($"[DEBUG] DefaultConnection available: {!string.IsNullOrEmpty(builder.Configuration.GetConnectionString("DefaultConnection"))}");
    }
}

if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.WriteLine("[DEBUG] Available configuration keys:");
    foreach (var item in builder.Configuration.AsEnumerable())
        if (item.Key.Contains("Connection", StringComparison.OrdinalIgnoreCase))
            Console.WriteLine($"  {item.Key} = {(item.Value?.Length > 0 ? "[SET]" : "[EMPTY]")}");
    throw new InvalidOperationException("Connection string not found. Define the appropriate Connection String for the current environment.");
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null)));

builder.Services.AddHttpClient<IFakeStoreApiService, FakeStoreApiService>(client =>
{
    var fakeStoreConfig = builder.Configuration.GetSection("FakeStoreApi");
    var baseUrl = fakeStoreConfig["BaseUrl"] ?? "https://fakestoreapi.com";
    client.BaseAddress = new Uri(baseUrl);
});

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IWishlistRepository, WishlistRepository>();
builder.Services.AddScoped<IReviewRepository, ReviewRepository>();
builder.Services.AddScoped<IExternalMappingRepository, ExternalMappingRepository>();
builder.Services.AddScoped<ICartRepository, CartRepository>();

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IWishlistService, WishlistService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IStoreService, StoreService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();

var jwtKey = configuration["Jwt:Key"]
          ?? configuration["Jwt_Key"]
          ?? Environment.GetEnvironmentVariable("Jwt_Key")
          ?? Environment.GetEnvironmentVariable("Jwt__Key");

if (string.IsNullOrWhiteSpace(jwtKey))
{
    Console.WriteLine("[ERROR] JWT Key not found in any configuration source");
    foreach (var item in configuration.AsEnumerable())
        if (item.Key.Contains("Jwt", StringComparison.OrdinalIgnoreCase))
            Console.WriteLine($"  {item.Key} = {(item.Value?.Length > 0 ? "[SET]" : "[EMPTY]")}");
    throw new InvalidOperationException("JWT key was not found in any valid location.");
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

builder.Services.AddCors(o =>
{
    o.AddPolicy("FrontPolicy", p =>
        p.WithOrigins("http://localhost:3000", "https://localhost:3000")
         .AllowAnyHeader()
         .AllowAnyMethod()
         .AllowCredentials());
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "TechTrendEmporium.Api", Version = "v1" });
    c.EnableAnnotations();
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization using the Bearer scheme. Enter 'Bearer' [space] and then your token.",
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

if (builder.Configuration.GetValue<bool>("EF:ApplyMigrationsOnStartup"))
{
    using var scope = app.Services.CreateScope();
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Setting up database...");
        if (builder.Environment.IsDevelopment())
        {
            logger.LogInformation("Creating/verifying development database...");
            await context.Database.EnsureCreatedAsync();
            logger.LogInformation("Development database created/verified successfully");
        }
        else
        {
            logger.LogInformation("Applying database migrations...");
            await context.Database.MigrateAsync();
            logger.LogInformation("Database migrations applied successfully");
        }
        await DbSeeder.SeedUsersAsync(context, logger);
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while setting up the database");
        if (app.Environment.IsProduction()) throw;
    }
}

var swaggerEnabled = builder.Configuration.GetValue<bool>("Swagger:Enabled", app.Environment.IsDevelopment());
if (swaggerEnabled)
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "TechTrendEmporium.Api v1");
        if (builder.Configuration.GetValue<bool>("Swagger:ServeAtRoot", false)) c.RoutePrefix = string.Empty;
    });
}

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

var forceHttpsRedirect = builder.Configuration.GetValue<bool>("Security:ForceHttpsRedirect", app.Environment.IsDevelopment());
if (forceHttpsRedirect)
{
    app.UseWhen(ctx => !ctx.Request.Path.StartsWithSegments("/health"),
        sub => sub.UseHttpsRedirection());
}

app.UseCors("FrontPolicy");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/", () => Results.Ok("OK"));
app.MapGet("/health", () => Results.Ok("OK"));

app.Run();
