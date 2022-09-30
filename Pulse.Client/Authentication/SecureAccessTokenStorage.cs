using Pulse.Core.Authentication;

namespace Pulse.Client.Authentication;

public class SecureAccessTokenStorage : IAccessTokenStorage
{
    private const string StorageKey = "access_token";
    private readonly ISecureStorage secureStorage;

    public SecureAccessTokenStorage(ISecureStorage secureStorage)
    {
        this.secureStorage = secureStorage;
        AccessToken = secureStorage.GetAsync(StorageKey).GetAwaiter().GetResult();
    }

    public string? AccessToken { get; private set; }
    
    public event Action? OnTokenChange;

    public async Task StoreAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        await secureStorage.SetAsync(StorageKey, accessToken);
        AccessToken = accessToken;
        OnTokenChange?.Invoke();
    }

    public Task RemoveTokenAsync(CancellationToken cancellationToken = default)
    {
        AccessToken = null;
        secureStorage.Remove(StorageKey);
        OnTokenChange?.Invoke();
        
        return Task.CompletedTask;
    }
}