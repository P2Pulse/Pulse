using Android.Media;
using AndroidStream = Android.Media.Stream;
using Stream = System.IO.Stream;

namespace Pulse.Client.Audio;

public class Microphone
{
    public async Task RecordAsync(Stream streamToWriteTo, CancellationToken ct)
    {
        var buffer = new byte[320];
        
        var recorder = new AudioRecord(AudioSource.VoiceCommunication, sampleRateInHz: 16_000, ChannelIn.Mono,
            Encoding.Pcm16bit, buffer.Length);
        
        recorder.StartRecording();
        
        while (!ct.IsCancellationRequested)
        {
            var read = await recorder.ReadAsync(buffer, 0, buffer.Length);
            await streamToWriteTo.WriteAsync(buffer, 0, read, ct);
        }
    }
}