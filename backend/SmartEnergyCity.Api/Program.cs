using Microsoft.EntityFrameworkCore;
using Npgsql;
using SmartEnergyCity.Api.Data;
using SmartEnergyCity.Api.Services;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddOpenApi();

builder.Services.AddDbContext<GameDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("PostgreSql")
        ?? "Host=localhost;Port=5432;Database=smart_energy_city;Username=postgres;******";
    try
    {
        _ = new NpgsqlConnectionStringBuilder(connectionString);
        options.UseNpgsql(connectionString);
    }
    catch
    {
        options.UseInMemoryDatabase("smart-energy-city");
    }
});

builder.Services.AddHttpClient<ITwoGisService, TwoGisService>(client =>
{
    var baseUrl = builder.Configuration["TwoGis:BaseUrl"] ?? "https://catalog.api.2gis.com";
    client.BaseAddress = new Uri(baseUrl);
});

builder.Services.AddScoped<IGameService, GameService>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5173")
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<GameDbContext>();
        await db.Database.EnsureCreatedAsync();
        var game = scope.ServiceProvider.GetRequiredService<IGameService>();
        await game.SeedBuildingsAsync();
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Database init failed. API can still run once PostgreSQL is available.");
    }
}

app.Run();
