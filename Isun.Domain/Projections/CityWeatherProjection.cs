using Isun.Domain.Dao;
using Isun.Domain.View;

namespace Isun.Domain.Projections;
public static class CityWeatherProjection
{
    public static CityWeatherDao ToDao(this CityWeatherView view)
    {
        return new CityWeatherDao
        {
            City = view.City,
            Temperature = view.Temperature,
            Precipitation = view.Precipitation,
            WindSpeed = view.WindSpeed,
            Summary = view.Summary
        };
    }
}
