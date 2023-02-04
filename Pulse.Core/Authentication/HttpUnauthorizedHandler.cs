using System.Net;

namespace Pulse.Core.Authentication;

internal class HttpUnauthorizedHandler : DelegatingHandler
{
    private readonly IAccessTokenStorage accessTokenStorage;

    public HttpUnauthorizedHandler(IAccessTokenStorage accessTokenStorage)
    {
        this.accessTokenStorage = accessTokenStorage;
    }
    
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, 
        CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        
        if (response.StatusCode is HttpStatusCode.Unauthorized)
            await accessTokenStorage.RemoveTokenAsync(cancellationToken).ConfigureAwait(false);
        
        return response;
    }
}