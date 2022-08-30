namespace Pulse.Core;

public interface ICaller
{
    Stream IncomingAudio { get; }
    Stream OutgoingAudio { get; }
}

internal class FakeCaller : ICaller, IAsyncDisposable
{
    public FakeCaller()
    {
        if (File.Exists("OutAudio.wav"))
            File.Delete("OutAudio.wav");

        OutgoingAudio = File.OpenWrite("OutAudio.wav");
        IncomingAudio = File.OpenRead("music.wav");
    }

    public Stream IncomingAudio { get; }
    public Stream OutgoingAudio { get; }

    public async ValueTask DisposeAsync()
    {
        await IncomingAudio.DisposeAsync();
        await OutgoingAudio.DisposeAsync();
    }
}