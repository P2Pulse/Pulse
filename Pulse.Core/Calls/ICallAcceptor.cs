namespace Pulse.Core.Calls;

public interface ICallAcceptor
{
    Task<Call> AnswerCallAsync(CancellationToken ct = default);
}