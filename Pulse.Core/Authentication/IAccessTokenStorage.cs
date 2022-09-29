namespace Pulse.Core.Authentication;

public interface IAccessTokenStorage
{
    string? AccessToken { get; }
    event Action OnTokenChange;
    Task StoreAsync(string accessToken, CancellationToken cancellationToken = default);
    Task RemoveTokenAsync(CancellationToken cancellationToken = default);
}