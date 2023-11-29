using Isun.Domain.View;
using Isun.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Isun.Services;
public interface ICitiesWeatherService
{
    Task<string[]> GetCities();

    Task<CityWeatherView?> GetWeather(string city);
}

public sealed class CitiesService : ICitiesWeatherService
{
    private readonly ILogger logger;
    private HttpClient httpClient;
    private readonly IConfiguration configuration;

    public CitiesService(ILoggerFactory loggerFactory,
                         IConfiguration configuration,
                         IHttpClientFactory clientFactory)
    {
        this.logger = loggerFactory.CreateLogger(nameof(CitiesService));
        this.httpClient = clientFactory.CreateClient(Constants.HttpClientForCityWeather);
        this.configuration = configuration;
    }

    public async Task<CityWeatherView?> GetWeather(string city)
    {
        try
        {
            var result = await this.httpClient.GetFromJsonAsync<CityWeatherView>($"api/weathers/get/{city}");
            return result;
        }
        catch (Exception e)
        {
            this.logger.LogError(e, "Method: {@Method}", nameof(GetWeather));
            return null;
        }
    }

    public async Task<string[]> GetCities()
    {
        try
        {
            var result = await this.httpClient.GetFromJsonAsync<string[]>($"api/cities/get");
            return result is null ? Array.Empty<string>() : result;
        }
        catch (Exception e)
        {
            this.logger.LogError(e, "Method: {@Method}", nameof(GetWeather));
            throw;
        }
    }
}
