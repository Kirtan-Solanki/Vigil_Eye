using VE.Shared.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() {
        Title = "Vigil Eye Auth & Pairing API",
        Version = "v1",
        Description = "VE-007: Authentication and device pairing service for Vigil Eye Phase 0"
    });
});

// VE-008: PostgreSQL connection
var connectionString = builder.Configuration.GetConnectionString("VigileEyeDb")
    ?? "Server=localhost;Port=5432;Database=vigileye;UserId=vigileye_dev;Password=changeme;";

builder.Services.AddDbContext<VigileEyeDbContext>(options =>
    options.UseNpgsql(connectionString));

// Add CORS for development
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevPolicy", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseCors("DevPolicy");
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new {
    service = "auth-pairing",
    status = "healthy",
    timestamp = DateTime.UtcNow
}));

app.Run();
