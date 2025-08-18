using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging; // Add this
using Serilog; // Add this

var builder = WebApplication.CreateBuilder(args);


// ✅ Add health check service
builder.Services.AddHealthChecks();

// Configure Serilog for file logging
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/log.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog(); // Add this

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddDbContext<WeatherDbContext>(options =>
    options.UseSqlite("Data Source=weather.db"));

var app = builder.Build();

// ✅ Map health check endpoint
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description
            })
        });
        await context.Response.WriteAsync(result);
    }
});

app.MapGet("/", () => "MinimalCloudAPI is running ✅");


// Seed database with initial data
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>(); // Get logger
    var db = scope.ServiceProvider.GetRequiredService<WeatherDbContext>();
    db.Database.Migrate();

    if (!db.WeatherForecasts.Any())
    {
        db.WeatherForecasts.AddRange(
            new WeatherForecast { Date = DateOnly.FromDateTime(DateTime.Now), TemperatureC = 25, Summary = "Sunny" },
            new WeatherForecast { Date = DateOnly.FromDateTime(DateTime.Now.AddDays(1)), TemperatureC = 22, Summary = "Cloudy" },
            new WeatherForecast { Date = DateOnly.FromDateTime(DateTime.Now.AddDays(2)), TemperatureC = 18, Summary = "Rainy" }
        );
        db.SaveChanges();
        logger.LogInformation("Seeded initial weather data."); // Log info
    }
    else
    {
        logger.LogInformation("Weather data already exists."); // Log info
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapGet("/weatherforecast", async (WeatherDbContext db) =>
{
    // Return all weather forecasts from the database
    // add logging to track the request
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Fetching weather forecasts from the database.");
    return await db.WeatherForecasts.ToListAsync();
})
.WithName("GetWeatherForecast");

app.Run();
