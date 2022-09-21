using Android.Media;
using AndroidStream = Android.Media.Stream;
using Stream = System.IO.Stream;

namespace Pulse.Client.Audio;

public class Speaker
{
    public async Task PlayAsync(Stream stream)
    {
        var buffer = new byte[320];
        
        var audioTrack = new AudioTrack(AndroidStream.Music, sampleRateInHz: 16_000, ChannelOut.Mono, Encoding.Pcm16bit, 
            buffer.Length, AudioTrackMode.Stream);
        
        audioTrack.Play();

        int bytesRead;
        while ((bytesRead = stream.Read(buffer)) != 0) 
            await audioTrack.WriteAsync(buffer, 0, bytesRead);
    }
}