namespace Pulse.Core.Calls;

public interface ICallInitiator
{
    Task<Stream> CallAsync(string username, CancellationToken ct = default);
}