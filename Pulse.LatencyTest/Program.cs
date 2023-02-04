using System.Diagnostics;
using NAudio.Wave;

Console.Write("Sensitivity: ");
var sensitivity = double.Parse(Console.ReadLine()!);

Console.Write("Do you want to send the ping or the pong? ");
var isPing = Console.ReadLine() == "ping";

var waveFormat = new WaveFormat(rate: 44100, bits: 16, channels: 1);
var audioIn = new WaveInEvent
{
    DeviceNumber = 0,
    WaveFormat = waveFormat
};

var stopwatch = new Stopwatch();

if (isPing) 
    await Task.Delay(5000);

var count = 0;
audioIn.DataAvailable += (_, args) =>
{
    try
    {
        var samples = new short[args.Buffer.Length / 2];
        Buffer.BlockCopy(args.Buffer, srcOffset: 0, samples, dstOffset: 0, args.Buffer.Length);

        var maxSample = samples.Max();
        var volume = (double)maxSample / short.MaxValue;

        if (volume > sensitivity && Interlocked.Increment(ref count) is 1)
        {
            
            
            if (isPing)
            {
                stopwatch.Stop();
                Console.WriteLine($"RTT: {stopwatch.ElapsedMilliseconds:0.000}ms");
            }

            var redundantSamples = samples.Length - Array.IndexOf(samples, maxSample) - 1;
            var redundantAudioLength = TimeSpan.FromSeconds(1) * redundantSamples / waveFormat.SampleRate;
            Console.WriteLine($"Processing Time: {redundantAudioLength.TotalMilliseconds:0.000}ms");
        }
        else if (count > 0)
        {
            audioIn.StopRecording();
            Thread.Sleep(1000);
            Console.Beep();
        }
    }
    catch (Exception e)
    {
        Console.WriteLine(e);
    }
};

audioIn.StartRecording();
await Task.Delay(1000);

if (isPing)
{
    stopwatch.Start();
    Console.Beep();
}

await Task.Delay(-1);