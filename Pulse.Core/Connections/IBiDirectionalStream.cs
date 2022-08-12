namespace Pulse.Core.Connections;

public interface IBiDirectionalStream : IAsyncDisposable
{
    Stream Input { get; }
    Stream Output { get; }
}