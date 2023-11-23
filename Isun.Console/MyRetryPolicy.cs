using Isun.Services;
using Isun.Shared;
using Microsoft.Extensions.Configuration;
using Polly;
using Polly.Retry;
using System.Net.Http.Headers;

namespace Isun;
public class MyRetryPolicy
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IAuthenticationService _authenticationService;

    public MyRetryPolicy(IHttpClientFactory httpClientFactory, IAuthenticationService authenticationService)
    {
        _httpClientFactory = httpClientFactory;
        this._authenticationService = authenticationService;
    }

    public AsyncRetryPolicy<HttpResponseMessage> GetRetryPolicy(IConfiguration configuration)
    {
        return Policy
            .HandleResult<HttpResponseMessage>(r => r.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            .RetryAsync(3, async (result, retryCount) =>
            {
                string userName = configuration["WeatherApi:UserName"]!;
                var newToken = await _authenticationService.GetBearerToken(userName, ArgsManager.Instance.Password);

                // Update the Authorization header for the provided HttpClient
                _httpClientFactory.CreateClient(Constants.HttpClientForAuthentication).DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", newToken);
            });
    }
}