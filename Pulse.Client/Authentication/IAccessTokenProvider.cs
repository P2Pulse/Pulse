namespace Pulse.Client.Authentication;

public interface IAccessTokenProvider
{
    string? AccessToken { get; }
}