using Isun.Domain.View;
using Isun.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace Isun.Services;

public interface IAuthenticationService
{
    Task<string> GetBearerToken(string userName, string password);
}
public sealed class AuthenticationService : IAuthenticationService
{
    private readonly ILogger logger;
    private readonly HttpClient httpClient;

    public AuthenticationService(ILoggerFactory loggerFactory, IHttpClientFactory clientFactory)
    {
        this.logger = loggerFactory.CreateLogger(nameof(AuthenticationService));
        this.httpClient = clientFactory.CreateClient(Constants.HttpClientForAuthentication);
    }

    public async Task<string> GetBearerToken(string userName, string password)
    {
        try
        {
            var httpResponse = await this.httpClient.PostAsJsonAsync("api/authenticate/Json", new AuthorizationRequestView(userName, password));
            if (httpResponse.IsSuccessStatusCode)
            {
                var result = await httpResponse.Content.ReadFromJsonAsync<AuthorizationResponseView>();

                if (result is null || string.IsNullOrEmpty(result.Token))
                {
                    this.logger.LogError("Method: {@Method}. Token is null or empty. UserName: {@UserName}. Password: {@Password}", nameof(GetBearerToken), userName, password);
                    throw new ApplicationException($"Method: {nameof(GetBearerToken)}. Token is null or empty");
                }

                return result.Token;
            }

            throw new HttpRequestException($"Method: {nameof(GetBearerToken)}. Http status code is not success: {httpResponse.StatusCode}");
        }
        catch (Exception e)
        {
            this.logger.LogError(e, "Method: {@Method}. Http status code is not success.", nameof(GetBearerToken));
            throw;
        }
    }
}
