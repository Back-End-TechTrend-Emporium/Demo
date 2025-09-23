using Microsoft.OpenApi.Models;
using Microsoft.EntityFrameworkCore;
using Back_End_TechTrend_Emporium.Data;
using Back_End_TechTrend_Emporium.Abstractions;

var builder = WebApplication.CreateBuilder(args);

// Add Entity Framework with retry on failure
builder.Services.AddDbContext<TodoDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("TodoDb"), 
        sqlOptions => sqlOptions.EnableRetryOnFailure()));

// Add repository
builder.Services.AddScoped<ITodoRepository, EfTodoRepository>();

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "TechTrendEmporium.Api", Version = "v1" });
});

var app = builder.Build();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<TodoDbContext>();
    context.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "TechTrendEmporium.Api v1");
    });
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
