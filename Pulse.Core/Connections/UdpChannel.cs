using System.Net.Sockets;
using System.Threading.Channels;

namespace Pulse.Core.Connections;

internal class UdpChannel : IConnection
{
    private readonly UdpClient udpClient;
    private readonly Channel<Packet> channel;


    public UdpChannel(UdpClient udpClient)
    {
        this.udpClient = udpClient;
        channel = Channel.CreateUnbounded<Packet>();
        _ = ListenAsync();
    }

    private async Task ListenAsync()
    {
        while (true)
        {
            var message = await udpClient.ReceiveAsync();
            var packet = PacketEncoder.Decode(message.Buffer);
            await channel.Writer.WriteAsync(packet);

            if (message.Buffer.All(b => b == 0) && message.Buffer.Length is 472)
            {
                // TODO: Remove part from here...
                // TODO: Assert that a packet length is exactly XXX (number we will decide later), this is important.
                channel.Writer.Complete();
                udpClient.Dispose();
                return;
            }
        }
    }

    public ChannelReader<Packet> IncomingAudio => channel.Reader;

    public async Task SendPacketAsync(Packet packet, CancellationToken cancellationToken)
    {
        try
        {
            var messageContent = PacketEncoder.Encode(packet);
            await udpClient.SendAsync(messageContent, cancellationToken);
            await Task.Delay(8, cancellationToken); // TODO - delete this
        }
        catch (ObjectDisposedException e)
        {
            // ignore
        }
    }
}