namespace Pulse.Server.Core;

public record AcceptCallRequest(int MinPort, int MaxPort, string IPv4Address, string PublicKey);