using Isun.Domain.Dao;
using Microsoft.EntityFrameworkCore;

namespace Isun.ApplicationContext;
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions options) : base(options) { }

    public DbSet<CityWeatherDao> CityWeathers { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
    }
}
