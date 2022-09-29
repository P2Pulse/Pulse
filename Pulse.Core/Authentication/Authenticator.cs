using System.Collections.Immutable;
using System.Net.Http.Json;

namespace Pulse.Core.Authentication;

public class Authenticator
{
    private readonly HttpClient server;
    private readonly IAccessTokenStorage tokenStorage;

    public Authenticator(HttpClient server, IAccessTokenStorage tokenStorage)
    {
        this.server = server;
        this.tokenStorage = tokenStorage;
    }

    public async Task<AuthenticationResult> AuthenticateAsync(string username, string password,
        CancellationToken cancellationToken = default)
    {
        using var response = await server.PostAsJsonAsync("/sessions", new
        {
            UserName = username,
            Password = password
        }, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadFromJsonAsync<SuccessfulAuthenticationResponse>(
                cancellationToken: cancellationToken);

            await tokenStorage.StoreAsync(responseContent!.AccessToken, cancellationToken);
            
            return new AuthenticationResult(Succeeded: true, Errors: ImmutableList<string>.Empty);
        }

        var errorResponses = await response.Content.ReadFromJsonAsync<ErrorResponse[]>(cancellationToken: cancellationToken);
        var errors = errorResponses!.Select(e => e.Description).ToImmutableList();
        return new AuthenticationResult(Succeeded: false, errors);
    }

    private record SuccessfulAuthenticationResponse(string AccessToken);
}