namespace Pulse.Core.Authentication;

public record AuthenticationResult(bool Succeeded, IReadOnlyList<string> Errors);