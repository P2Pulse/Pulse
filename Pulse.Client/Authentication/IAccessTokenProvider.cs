namespace Pulse.Client.Authentication;

public interface IAccessTokenProvider
{
    string? AccessToken { get; }
}

internal class HardCodedAccessTokenProvider : IAccessTokenProvider
{
    public string? AccessToken => "GHJFGIHJGFH";
}