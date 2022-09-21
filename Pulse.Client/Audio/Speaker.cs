using Android.Media;
using AndroidStream = Android.Media.Stream;
using Stream = System.IO.Stream;

namespace Pulse.Client.Audio;

public class Speaker
{
    private async ValueTask PlayAsync(Stream stream)
    {
        var audioTrack = new AudioTrack(AndroidStream.Music, 16_000, ChannelOut.Mono, Encoding.Pcm16bit, 320,
            AudioTrackMode.Stream);
        audioTrack.Play();

        var buffer = new byte[320];
        int bytesRead;
        while ((bytesRead = stream.Read(buffer)) != 0)
        {
            await audioTrack.WriteAsync(buffer, 0, bytesRead);
        }
    }
}