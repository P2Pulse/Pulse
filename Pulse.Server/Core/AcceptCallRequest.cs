namespace Pulse.Server.Core;

public record AcceptCallRequest(int MinPort, int MaxPort, string calleeIPv4Address);