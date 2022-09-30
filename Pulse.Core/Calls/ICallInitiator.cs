namespace Pulse.Core.Calls;

public interface ICallInitiator
{
    Task<Call> CallAsync(string username, CancellationToken ct = default);
}