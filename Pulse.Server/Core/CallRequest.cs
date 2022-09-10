namespace Pulse.Server.Core;

public record CallRequest(bool calling, string? username=null);