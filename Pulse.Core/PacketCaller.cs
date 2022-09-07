using Pulse.Core.AudioStreaming;

namespace Pulse.Core;

internal class PacketCaller : ICaller
{
    private readonly StreamEstablisher streamEstablisher;

    public PacketCaller(StreamEstablisher streamEstablisher)
    {
        this.streamEstablisher = streamEstablisher;
    }
    
    public async Task<Call> CallAsync(string calleeUsername, CancellationToken cancellationToken = default)
    {
        /*var stream = await streamEstablisher.EstablishStreamAsync(calleeUsername, cancellationToken);
        return new Call(incomingAudio: stream, outgoingAudio: stream);*/
        throw new NotImplementedException();
    }
}