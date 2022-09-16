using System.Net;
using Pulse.Core.AudioStreaming;
using Pulse.Core.Connections;

namespace Pulse.Core.Calls;

internal class UdpStreamFactory
{
    public async Task<Stream> ConnectAsync(
        Func<RequestConnectionInfo, Task<ConnectionInfo>> exchangeConnectionInfo,
        CancellationToken cancellationToken = default
    )
    {
        var portBruteForcer = new PortBruteForceNatTraversal();
        var (myIPv4Address, min, max) = await portBruteForcer.PredictMinMaxPortsAsync(
            cancellationToken
        );

        var packetEncryptor = new PacketEncryptor();
        var myInfo = new RequestConnectionInfo(
            myIPv4Address.ToString(),
            min,
            max,
            packetEncryptor.PublicKey
        );

        var connectionInfo = await exchangeConnectionInfo(myInfo);

        var connection = await portBruteForcer.EstablishConnectionAsync(
            IPAddress.Parse(connectionInfo.remoteIPAddress),
            connectionInfo.MinPort,
            connectionInfo.MaxPort,
            cancellationToken
        );

        packetEncryptor.SetAesIV(connectionInfo.IV!);
        packetEncryptor.SetOtherPartyPublicKey(connectionInfo.PublicKey);

        connection = new OutOfOrderDropper(connection);
        connection = new EncryptedConnection(connection, packetEncryptor);

        return new PacketStream(connection);
    }
}