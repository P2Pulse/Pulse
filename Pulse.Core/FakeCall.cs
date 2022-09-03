namespace Pulse.Core;

internal class FakeCaller : ICaller
{
    public Task<Call> CallAsync(string calleeUsername, CancellationToken cancellationToken = default)
    {
        if (File.Exists("OutAudio.wav"))
            File.Delete("OutAudio.wav");

        var outgoingAudio = File.OpenWrite("OutAudio.wav");
        var incomingAudio = File.OpenRead("music.wav");
        
        return Task.FromResult(new Call(incomingAudio, outgoingAudio));
    }
}