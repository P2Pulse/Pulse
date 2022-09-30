using Android.Media;
using AndroidStream = Android.Media.Stream;
using Stream = System.IO.Stream;

namespace Pulse.Client.Audio;

public class Speaker
{
    public async Task PlayAsync(Stream stream)
    {
        var buffer = new byte[320];
        var audioAttributes = new AudioAttributes.Builder().SetContentType(AudioContentType.Speech)?.SetUsage(AudioUsageKind.VoiceCommunication)?.Build();
        var audioFormat = new AudioFormat.Builder()
            .SetEncoding(Encoding.Pcm16bit)?
            .SetSampleRate(16_000)?
            .SetChannelMask(ChannelOut.Mono).Build();

        var audioTrack = new AudioTrack(audioAttributes, audioFormat, bufferSizeInBytes: buffer.Length, AudioTrackMode.Stream, 0);
        // var audioTrack = new AudioTrack(AndroidStream.VoiceCall, sampleRateInHz: 16_000, ChannelOut.Mono, Encoding.Pcm16bit,
            // buffer.Length, AudioTrackMode.Stream);

        audioTrack.Play();

        int bytesRead;
        while ((bytesRead = await stream.ReadAsync(buffer)) != 0)
            await audioTrack.WriteAsync(buffer, 0, bytesRead);
    }
}