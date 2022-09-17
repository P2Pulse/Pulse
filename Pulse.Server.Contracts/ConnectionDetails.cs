namespace Pulse.Server.Contracts;

public record ConnectionDetails(
    string IPAddress,
    int MinPort,
    int MaxPort,
    byte[] PublicKey,
    byte[] IV);