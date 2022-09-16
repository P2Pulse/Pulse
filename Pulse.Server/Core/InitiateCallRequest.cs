namespace Pulse.Server.Core;

public record InitiateCallRequest(string CalleeUserName, int MinPort, int MaxPort, string IPv4Address, string PublicKey);