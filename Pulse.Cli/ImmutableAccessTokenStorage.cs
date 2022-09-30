using Pulse.Core.Authentication;

namespace Pulse.Cli;

internal class ImmutableAccessTokenStorage : IAccessTokenStorage
{
    public ImmutableAccessTokenStorage(string accessToken)
    {
        AccessToken = accessToken;
    }
    public string? AccessToken { get; }
    public event Action? OnTokenChange;
    public Task StoreAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    public Task RemoveTokenAsync(CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }
}