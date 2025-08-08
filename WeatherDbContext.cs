using Microsoft.EntityFrameworkCore;

public class WeatherDbContext : DbContext
{
    public WeatherDbContext(DbContextOptions<WeatherDbContext> options)
        : base(options) { }

    public DbSet<WeatherForecast> WeatherForecasts { get; set; }
}