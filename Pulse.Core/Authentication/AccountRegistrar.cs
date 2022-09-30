using System.Collections.Immutable;
using System.Net.Http.Json;

namespace Pulse.Core.Authentication;

public class AccountRegistrar
{
    private readonly HttpClient server;

    public AccountRegistrar(HttpClient server)
    {
        this.server = server;
    }

    public async Task<AuthenticationResult> RegisterAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        using var response = await server.PostAsJsonAsync("/accounts", new
        {
            UserName = username,
            Password = password
        }, cancellationToken);

        if (response.IsSuccessStatusCode)
            return new AuthenticationResult(Succeeded: true, Errors: ImmutableList<string>.Empty);

        var errorResponses = await response.Content.ReadFromJsonAsync<ErrorResponse[]>(cancellationToken: cancellationToken);
        var errors = errorResponses!.Select(e => e.Description).ToImmutableList();
        return new AuthenticationResult(Succeeded: false, errors);
    }
}