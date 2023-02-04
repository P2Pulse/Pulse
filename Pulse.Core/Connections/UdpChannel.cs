using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;

namespace Pulse.Core.Connections;

internal class UdpChannel : IConnection
{
    private readonly UdpClient udpClient;
    private readonly Channel<Packet> channel;
    private readonly CancellationTokenSource backgroundListening = new();

    public UdpChannel(UdpClient udpClient)
    {
        this.udpClient = udpClient;
        channel = Channel.CreateUnbounded<Packet>();
        _ = ListenAsync(backgroundListening.Token);
    }

    private async Task ListenAsync(CancellationToken cancellationToken)
    {
        cancellationToken.Register(() => channel.Writer.Complete());
        cancellationToken.Register(() => udpClient.Dispose());
        
        while (!cancellationToken.IsCancellationRequested)
        {
            var message = await udpClient.ReceiveAsync(cancellationToken).ConfigureAwait(false);
            
            var textualContent = Encoding.ASCII.GetString(message.Buffer);
            if (textualContent.StartsWith("Knockout") || textualContent.StartsWith("Punch!"))
                continue; // Ignore internal messages that come from the initiation phase

            var packet = PacketEncoder.Decode(message.Buffer);

            await channel.Writer.WriteAsync(packet, cancellationToken).ConfigureAwait(false);
        }
    }

    public ChannelReader<Packet> IncomingAudio => channel.Reader;

    public async Task SendPacketAsync(Packet packet, CancellationToken cancellationToken)
    {
        try
        {
            var messageContent = PacketEncoder.Encode(packet);
            await udpClient.SendAsync(messageContent, cancellationToken).ConfigureAwait(false);
            await Task.Delay(8, cancellationToken).ConfigureAwait(false); // TODO - delete this
        }
        catch (ObjectDisposedException)
        {
            // ignore
        }
    }

    public ValueTask DisposeAsync()
    {
        backgroundListening.Cancel();
        
        channel.Writer.TryComplete();
        
        return ValueTask.CompletedTask;
    }
}