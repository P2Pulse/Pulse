using Android.Media;
using Android.Media.Audiofx;
using AndroidStream = Android.Media.Stream;
using Stream = System.IO.Stream;

namespace Pulse.Client.Audio;

public class Microphone
{
    private readonly AudioRecord recorder;
    private const int BufferLength = 320;

    public int audioSessionId { get; }

    public Microphone()
    {
        recorder = new AudioRecord(AudioSource.VoiceCommunication, sampleRateInHz: 16_000, ChannelIn.Mono,
            Encoding.Pcm16bit, BufferLength);
        audioSessionId = recorder.AudioSessionId;
    }


    public async Task RecordAsync(Stream streamToWriteTo, CancellationToken ct)
    {
        recorder.StartRecording();
        var buffer = new byte[BufferLength];
        while (!ct.IsCancellationRequested)
        {
            var read = await recorder.ReadAsync(buffer, 0, buffer.Length);
            await streamToWriteTo.WriteAsync(buffer, 0, read, ct);
        }
    }
}