using Isun.Domain.View;
using Isun.Shared;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Isun.Services;
public interface ICitiesWeatherService
{
    void Init(string token);

    Task<string[]> GetCities();

    Task<CityWeatherView?> GetWeather(string city);
}

public sealed class CitiesService : ICitiesWeatherService
{
    private ILogger logger;
    private readonly HttpClient httpClient;

    public CitiesService(ILoggerFactory loggerFactory, IHttpClientFactory clientFactory)
    {
        this.logger = loggerFactory.CreateLogger(nameof(CitiesService));
        this.httpClient = clientFactory.CreateClient(Constants.HttpClientForAuthentication);
        this.httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "");
    }

    public void Init(string token)
    {
        this.httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<CityWeatherView?> GetWeather(string city)
    {
        try
        {
            var result = await this.httpClient.GetFromJsonAsync<CityWeatherView>($"api/weathers/{city}");
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
            var result = await this.httpClient.GetFromJsonAsync<string[]>($"api/cities");

            if (result is null || result.Length == 0)
            {
                throw new ApplicationException($"Method: {nameof(GetCities)}. No cities in response");
            }

            return result;
        }
        catch (Exception e)
        {
            this.logger.LogError(e, "Method: {@Method}", nameof(GetWeather));
            throw;
        }
    }
}
