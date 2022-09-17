using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;
using Pulse.Core.Connections;

namespace Pulse.Core.AudioStreaming;

internal class CallHanger : IConnection
{
    private readonly IConnection actualConnection;
    private readonly CancellationTokenSource cancellationTokenSource;


    public CallHanger(IConnection actualConnection)
    {
        cancellationTokenSource = new CancellationTokenSource();
        IncomingAudio = new CallHangerChannelReader(actualConnection.IncomingAudio, this, cancellationTokenSource.Token);
        this.actualConnection = actualConnection;
    }

    public ChannelReader<Packet> IncomingAudio { get; }

    public async Task SendPacketAsync(Packet packet, CancellationToken cancellationToken)
    {
        await actualConnection.SendPacketAsync(packet, cancellationToken);
    }

    private class CallHangerChannelReader : ChannelReader<Packet>
    {
        private readonly ChannelReader<Packet> actualChannelReader;
        private readonly CallHanger callHanger;
        private DateTimeOffset lastPacketReceivedAt;
        private bool connectionLost;

        public CallHangerChannelReader(ChannelReader<Packet> actualChannelReader, CallHanger callHanger, CancellationToken ct)
        {
            this.actualChannelReader = actualChannelReader;
            this.callHanger = callHanger;
            connectionLost = false;

            lastPacketReceivedAt = DateTimeOffset.Now;
            _ = HangUpIfNoResponseForTooLongAsync(ct);
        }

        private async Task HangUpIfNoResponseForTooLongAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                if (DateTimeOffset.Now - lastPacketReceivedAt > TimeSpan.FromSeconds(5))
                {
                    Console.WriteLine("Assuming connection lost, hanging up.");
                    connectionLost = true;
                    await callHanger.DisposeAsync();
                    break;
                }

                await Task.Delay(TimeSpan.FromSeconds(1), ct);
            }
        }

        public override bool TryRead([MaybeNullWhen(returnValue: false)] out Packet packet)
        {
            if (!actualChannelReader.TryRead(out packet))
                return false;

            if (connectionLost)
                return false;

            lastPacketReceivedAt = DateTimeOffset.Now;

            if (packet.SerialNumber == int.MaxValue && packet.Content.Length is 420 &&
                packet.Content.ToArray().All(b => b == 0))
            {
                Console.WriteLine("Other side hanged up.");
                _ = callHanger.DisposeAsync(); // yuck
                return false;
            }

            return true;
        }

        public override ValueTask<bool> WaitToReadAsync(CancellationToken cancellationToken = default)
        {
            return actualChannelReader.WaitToReadAsync(cancellationToken);
        }
    }

    public async ValueTask DisposeAsync()
    {
        // send an hangup packet
        cancellationTokenSource.Cancel();
        await actualConnection.SendPacketAsync(new Packet(int.MaxValue, new byte[420]), CancellationToken.None);
        await actualConnection.DisposeAsync();
    }
}