using Isun.Domain.Dao;
using Microsoft.Extensions.Logging;

namespace Isun.ApplicationContext;

public interface ICitiesRepository
{
    Task AddRange(List<CityWeatherDao> items);
}

public sealed class CitiesRepository : ICitiesRepository
{
    private readonly ILogger<CitiesRepository> logger;
    private readonly ApplicationDbContext context;

    public CitiesRepository(ILogger<CitiesRepository> logger,
                            ApplicationDbContext context)
    {
        this.logger = logger;
        this.context = context;
    }

    public async Task AddRange(List<CityWeatherDao> items)
    {
        try
        {
            context.AddRange(items);
            await context.SaveChangesAsync();
        }
        catch (Exception e)
        {
            this.logger.LogError(e, "Method: {@Method}", nameof(AddRange));
            throw;
        }
    }
}
