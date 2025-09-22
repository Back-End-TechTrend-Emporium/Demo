using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using TechTrendEmporium.Core.Entities;
using TechTrendEmporium.Core.Interfaces;
using TechTrendEmporium.Infrastructure.Data;
using TechTrendEmporium.Infrastructure.Repositories;
using TechTrendEmporium.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Data Source=LocalDB;Integrated Security=true;Database=TechTrendEmporium";

// Entity Framework
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (builder.Environment.IsDevelopment())
    {
        options.UseInMemoryDatabase("TechTrendEmporium");
    }
    else
    {
        options.UseSqlServer(connectionString);
    }
});

// Identity
builder.Services.AddIdentity<User, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? "your-256-bit-secret-key-here-for-development-only";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "TechTrendEmporium";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "TechTrendEmporium";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

// Authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin", "SuperAdmin"));
    options.AddPolicy("EmployeeOnly", policy => policy.RequireRole("Employee", "Admin", "SuperAdmin"));
    options.AddPolicy("SuperAdminOnly", policy => policy.RequireRole("SuperAdmin"));
});

// Repository registrations
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<ICartRepository, CartRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IReviewRepository, ReviewRepository>();
builder.Services.AddScoped<IWishlistRepository, WishlistRepository>();
builder.Services.AddScoped<ICouponRepository, CouponRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// HTTP Client for FakeStore API
builder.Services.AddHttpClient<IFakeStoreApiService, FakeStoreApiService>();

// Controllers
builder.Services.AddControllers();

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "TechTrend Emporium API", 
        Version = "v1",
        Description = "E-commerce API for TechTrend Emporium platform"
    });
    
    // JWT Authorization in Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token",
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
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "TechTrend Emporium API V1");
        c.RoutePrefix = string.Empty; // Serve Swagger UI at root
    });
}

app.UseHttpsRedirection();
app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Initialize database and seed data
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    
    await InitializeDatabase(context, userManager, roleManager);
}

app.Run();

async Task InitializeDatabase(ApplicationDbContext context, UserManager<User> userManager, RoleManager<IdentityRole> roleManager)
{
    // Ensure database is created
    await context.Database.EnsureCreatedAsync();
    
    // Create roles
    var roles = new[] { "SuperAdmin", "Admin", "Employee", "Shopper" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }
    
    // Create default admin user
    const string adminEmail = "admin@techtrendemporium.com";
    const string adminPassword = "Admin123!";
    
    var adminUser = await userManager.FindByEmailAsync(adminEmail);
    if (adminUser == null)
    {
        adminUser = new User
        {
            UserName = adminEmail,
            Email = adminEmail,
            FirstName = "System",
            LastName = "Administrator",
            EmailConfirmed = true
        };
        
        var result = await userManager.CreateAsync(adminUser, adminPassword);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, "SuperAdmin");
        }
    }
}
