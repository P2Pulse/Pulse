using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Pulse.Core.Connections;

public class PortBruteForceNatTraversal : IConnectionEstablishmentStrategy
{
    private const int SourcePort = 54551;

    public async Task<IConnection> EstablishConnectionAsync(IPAddress destination, 
        CancellationToken cancellationToken = default)
    {
        var shouldSend = true;
        using var client = new UdpClient(SourcePort);
        _ = client.ReceiveAsync(cancellationToken)
            .AsTask().ContinueWith(task =>
            {
                shouldSend = false;
                Console.WriteLine("Message received: " + Encoding.ASCII.GetString(task.Result.Buffer));
            }, cancellationToken);

        for (var i = 0; shouldSend; i++)
        {
            await destination.SendMessageAsync(client, i % ushort.MaxValue);
        }

        return null;
    }
}