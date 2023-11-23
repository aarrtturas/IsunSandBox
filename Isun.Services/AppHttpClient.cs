using Isun.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Isun.Services;

public interface IAppHttpClient
{
    Task<T?> GetAsync<T>(HttpClient httpClient, string endpoint);
}

public sealed class AppHttpClient : IAppHttpClient
{
    private readonly ILogger logger;
    private readonly IConfiguration configuration;
    private readonly IAuthenticationService authenticationService;

    public AppHttpClient(ILoggerFactory loggerFactory,
                         IConfiguration configuration,
                         IAuthenticationService authenticationService)
    {
        this.logger = loggerFactory.CreateLogger(nameof(AppHttpClient));
        this.configuration = configuration;
        this.authenticationService = authenticationService;
    }

    public async Task<T?> GetAsync<T>(HttpClient httpClient, string endpoint)
    {
        try
        {
            AsyncRetryPolicy<HttpResponseMessage> retryPolicy = GetRetryPolicy(httpClient);

            var response = await retryPolicy.ExecuteAsync(async () =>
            {
                var result = await httpClient.GetAsync(endpoint);
                return result;
            });

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<T>();
            }
            else
            {
                this.logger.LogWarning("Method: {@Method}. Status: {@Status}", nameof(GetAsync), response.StatusCode);
                return default(T);
            }
        }
        catch (Exception e)
        {
            this.logger.LogError(e, "Method: {@Method}", nameof(GetAsync));
            return default(T);
        }
    }

    public AsyncRetryPolicy<HttpResponseMessage> GetRetryPolicy(HttpClient httpClient)
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
