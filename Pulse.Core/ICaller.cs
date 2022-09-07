namespace Pulse.Core;

public interface ICaller
{
    Task<Call> CallAsync(string calleeUsername, CancellationToken cancellationToken = default);
}