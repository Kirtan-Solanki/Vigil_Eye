using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using VE.Shared.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() {
        Title = "Vigil Eye Ingestion API",
        Version = "v1",
        Description = "VE-007: Event ingestion service for Vigil Eye Phase 0"
    });
});

// VE-008: PostgreSQL connection
var connectionString = builder.Configuration.GetConnectionString("VigileEyeDb")
    ?? "Server=localhost;Port=5432;Database=vigileye;UserId=vigileye_dev;Password=changeme;";

builder.Services.AddDbContext<VigileEyeDbContext>(options =>
    options.UseNpgsql(connectionString));

// Redis for message queue
var redisConnection = builder.Configuration["Redis:ConnectionString"] ?? "localhost:6379";
builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConnection));

builder.Services.AddCors(options =>
{
    options.AddPolicy("DevPolicy", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseCors("DevPolicy");
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/health", () => Results.Ok(new {
    service = "ingestion",
    status = "healthy",
    timestamp = DateTime.UtcNow
}));

app.Run();
