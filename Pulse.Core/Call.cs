namespace Pulse.Core;

public class Call : IAsyncDisposable
{
    public Call(Stream incomingAudio, Stream outgoingAudio)
    {
        IncomingAudio = incomingAudio;
        OutgoingAudio = outgoingAudio;
    }

    public Stream IncomingAudio { get; }
    public Stream OutgoingAudio { get; }
    
    
    public async ValueTask DisposeAsync()
    {
        await IncomingAudio.DisposeAsync();
        await OutgoingAudio.DisposeAsync();
    }
}