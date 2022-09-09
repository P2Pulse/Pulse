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
            var packet = new Packet(message.Buffer);
            await channel.Writer.WriteAsync(packet);
            
            if (message.Buffer.All(b => b == 0))
            {
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
            await udpClient.SendAsync(packet.Content, cancellationToken);
        }
        catch (ObjectDisposedException e) 
        {
            // ignore
        }
    }
}