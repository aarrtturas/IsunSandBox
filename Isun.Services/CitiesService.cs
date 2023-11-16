using Isun.Domain.View;
using Isun.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using System.Net.Http;
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
    private readonly ILogger logger;
    private HttpClient httpClient;
    private readonly IConfiguration configuration;
    private readonly IAuthenticationService authenticationService;

    public CitiesService(ILoggerFactory loggerFactory,
                         IConfiguration configuration,
                         IHttpClientFactory clientFactory,
                         IAuthenticationService authenticationService)
    {
        this.logger = loggerFactory.CreateLogger(nameof(CitiesService));
        this.httpClient = clientFactory.CreateClient(Constants.HttpClientForCityWeather);
        this.configuration = configuration;
        this.authenticationService = authenticationService;
    }

    public void Init(string token)
    {
        this.httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<CityWeatherView?> GetWeather(string city)
    {
        try
        {
            AsyncRetryPolicy<HttpResponseMessage> retryPolicy = GetRetryPolicy();

            var response = await retryPolicy.ExecuteAsync(async () =>
            {
                var result = await this.httpClient.GetAsync($"api/weathers/{city}");
                return result;
            });

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<CityWeatherView>();
            }
            else
            {
                this.logger.LogWarning("Method: {@Method}. Status: {@Status}", nameof(GetWeather), response.StatusCode);
                return null;
            }
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
            AsyncRetryPolicy<HttpResponseMessage> retryPolicy = GetRetryPolicy();

            var response = await retryPolicy.ExecuteAsync(async () =>
            {
                var result = await this.httpClient.GetAsync($"api/cities");
                return result;
            });

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<string[]>();
                if (result is null || result.Length == 0)
                {
                    this.logger.LogWarning("Method: {@Method}. No cities in response", nameof(GetWeather));
                    return Array.Empty<string>();
                }

                return result;
            }
            else
            {
                this.logger.LogWarning("Method: {@Method}. Status: {@Status}", nameof(GetWeather), response.StatusCode);
                return Array.Empty<string>();
            }
        }
        catch (Exception e)
        {
            this.logger.LogError(e, "Method: {@Method}", nameof(GetWeather));
            throw;
        }
    }

    public AsyncRetryPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return Policy
        .HandleResult<HttpResponseMessage>(r => r.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        .RetryAsync(3, async (result, retryCount) =>
        {

            string userName = configuration["WeatherApi:UserName"]!;
            var newToken = await authenticationService.GetBearerToken(userName, ArgsManager.Instance.Password);
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", newToken);
        });
    }
}
