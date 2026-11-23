using System.Net;

namespace InventoryApp.Client.Services;

public class SessionExpiryHandler : DelegatingHandler
{
    private readonly UserSession _session;

    public SessionExpiryHandler(UserSession session)
    {
        _session = session;
        InnerHandler = new HttpClientHandler();
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);

        var isAuthEndpoint = request.RequestUri?.AbsolutePath
            .StartsWith("/api/auth/", StringComparison.OrdinalIgnoreCase) ?? false;

        if (response.StatusCode == HttpStatusCode.Unauthorized && !isAuthEndpoint && _session.IsLoggedIn)
            _session.Expire();

        return response;
    }
}