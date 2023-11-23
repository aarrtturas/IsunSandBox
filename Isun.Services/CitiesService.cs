using Isun.Domain.View;
using Isun.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;

namespace Isun.Services;
public interface ICitiesWeatherService
{
    void Init(string token);

    Task<string[]> GetCities();

    Task<CityWeatherView?> GetWeather(string city);
}

public sealed class CitiesService : ICitiesWeatherService
{
    private readonly ILogger logger;
    private HttpClient httpClient;
    private readonly IConfiguration configuration;
    private readonly IAppHttpClient appHttpClient;

    public CitiesService(ILoggerFactory loggerFactory,
                         IConfiguration configuration,
                         IHttpClientFactory clientFactory,
                         IAppHttpClient appHttpClient)
    {
        this.logger = loggerFactory.CreateLogger(nameof(CitiesService));
        this.httpClient = clientFactory.CreateClient(Constants.HttpClientForCityWeather);
        this.configuration = configuration;
        this.appHttpClient = appHttpClient;
    }

    public void Init(string token)
    {
        this.httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<CityWeatherView?> GetWeather(string city)
    {
        try
        {
            var result = await this.appHttpClient.GetAsync<CityWeatherView>(this.httpClient, $"api/weather/{city}");
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
            var result = await this.appHttpClient.GetAsync<string[]>(this.httpClient, $"api/cities");
            return result is null ? Array.Empty<string>() : result;
        }
        catch (Exception e)
        {
            this.logger.LogError(e, "Method: {@Method}", nameof(GetWeather));
            throw;
        }
    }
}
