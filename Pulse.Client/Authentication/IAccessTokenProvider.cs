namespace Pulse.Client.Authentication;

public interface IAccessTokenProvider
{
    string? AccessToken { get; }
}

class HardCodedAccessTokenProvider : IAccessTokenProvider
{
    public string? AccessToken => "GHJFGIHJGFH";
}