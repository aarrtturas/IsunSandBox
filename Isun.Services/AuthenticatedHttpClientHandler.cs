using Isun.Shared;

namespace Isun.Services;
public sealed class AuthenticatedHttpClientHandler : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.Add("Authorization", $"Bearer {TokenManager.Instance.Token}");

        return base.SendAsync(request, cancellationToken);
    }
}