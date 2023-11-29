using Isun.Shared;
using System.Net.Http.Headers;

namespace Isun.Services;
public sealed class AuthenticatedHttpClientHandler : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", TokenManager.Instance.Token);

        return base.SendAsync(request, cancellationToken);
    }
}