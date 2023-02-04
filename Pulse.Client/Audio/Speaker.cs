using Android.Media;
using Android.Media.Audiofx;
using AndroidStream = Android.Media.Stream;
using Stream = System.IO.Stream;

namespace Pulse.Client.Audio;

public class Speaker
{
    private const int BufferLength = 320;

    public async Task PlayAsync(Stream stream)
    {
        var audioAttributes = new AudioAttributes.Builder().SetContentType(AudioContentType.Speech)?.SetUsage(AudioUsageKind.VoiceCommunication)?.Build();
        var audioFormat = new AudioFormat.Builder()
            .SetEncoding(Encoding.Pcm16bit)?
            .SetSampleRate(16_000)?
            .SetChannelMask(ChannelOut.Mono).Build();

        var audioTrack = new AudioTrack(audioAttributes, audioFormat, bufferSizeInBytes: BufferLength, AudioTrackMode.Stream, 0);
        var buffer = new byte[BufferLength];
        audioTrack.Play();
        
        int bytesRead;
        while ((bytesRead = await stream.ReadAsync(buffer)) != 0)
            await audioTrack.WriteAsync(buffer, 0, bytesRead);
    }
}