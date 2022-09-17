namespace Pulse.Server.Contracts;

public record JoinCallRequest(int MinPort, int MaxPort, string IPAddress, byte[] PublicKey);