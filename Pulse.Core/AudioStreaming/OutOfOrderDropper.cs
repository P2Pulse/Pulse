using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;
using Pulse.Core.Connections;

namespace Pulse.Core.AudioStreaming;

/// <summary>
/// A connection that drops packets that arrive out-of-order
/// </summary>
internal class OutOfOrderDropper : IConnection
{
    private readonly IConnection actualConnection;

    public OutOfOrderDropper(IConnection actualConnection)
    {
        IncomingAudio = new PacketDroppingChannelReader(actualConnection.IncomingAudio);
        this.actualConnection = actualConnection;
    }

    public ChannelReader<Packet> IncomingAudio { get; }

    public async Task SendPacketAsync(Packet packet, CancellationToken cancellationToken)
    {
        await actualConnection.SendPacketAsync(packet, cancellationToken).ConfigureAwait(false);
    }

    private class PacketDroppingChannelReader : ChannelReader<Packet>
    {
        private readonly ChannelReader<Packet> actualChannelReader;
        private int lastProcessedPacket;

        public PacketDroppingChannelReader(ChannelReader<Packet> actualChannelReader)
        {
            this.actualChannelReader = actualChannelReader;
        }

        public override bool TryRead([MaybeNullWhen(returnValue: false)] out Packet packet)
        {
            var firstIteration = true;
            do
            {
                if (!actualChannelReader.TryRead(out packet))
                    return false;
                if (!firstIteration)
                    Console.WriteLine("Dropped");
                else  // TODO: remove me!
                    firstIteration = false;
            } while (packet.SerialNumber <= lastProcessedPacket);

            lastProcessedPacket = packet.SerialNumber;

            return true;
        }

        public override ValueTask<bool> WaitToReadAsync(CancellationToken cancellationToken = default)
        {
            return actualChannelReader.WaitToReadAsync(cancellationToken);
        }
    }

    public ValueTask DisposeAsync()
    {
        return actualConnection.DisposeAsync();
    }
}