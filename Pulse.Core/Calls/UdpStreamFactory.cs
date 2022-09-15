using System.Net;
using Pulse.Core.AudioStreaming;
using Pulse.Core.Connections;

namespace Pulse.Core.Calls;

internal class UdpStreamFactory
{
    public async Task<Stream> ConnectAsync(Func<ConnectionInfo, Task<ConnectionInfo>> exchangeConnectionInfo, 
        CancellationToken cancellationToken = default)
    {
        var connection = await EstablishP2PConnectionAsync(exchangeConnectionInfo, cancellationToken);
        connection = new OutOfOrderDropper(connection);
        return new PacketStream(connection);
    }

    private static async Task<IConnection> EstablishP2PConnectionAsync(Func<ConnectionInfo, Task<ConnectionInfo>> exchangeConnectionInfo, CancellationToken cancellationToken)
    {
        var portBruteForcer = new PortBruteForceNatTraversal();
        var (myIPv4Address, min, max) = await portBruteForcer.PredictMinMaxPortsAsync(cancellationToken);
        var myInfo = new ConnectionInfo(myIPv4Address.ToString(), min, max);

        var (remoteIpAddress, minPort, maxPort) = await exchangeConnectionInfo(myInfo);

        return await portBruteForcer.EstablishConnectionAsync(
            IPAddress.Parse(remoteIpAddress), minPort, maxPort, cancellationToken);
    }
}