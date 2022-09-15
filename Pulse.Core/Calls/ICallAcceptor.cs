namespace Pulse.Core.Calls;

public interface ICallAcceptor
{
    Task<Stream> AnswerCallAsync(CancellationToken ct = default);
}