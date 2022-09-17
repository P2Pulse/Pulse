using System.Net;
using Pulse.Core.AudioStreaming;
using Pulse.Core.Connections;
using Pulse.Server.Contracts;

namespace Pulse.Core.Calls;

internal class UdpStreamFactory
{
    public async Task<Stream> ConnectAsync(
        Func<JoinCallRequest, Task<ConnectionDetails>> exchangeConnectionInfo,
        CancellationToken cancellationToken = default
    )
    {
        var portBruteForcer = new PortBruteForceNatTraversal();
        var (myIPv4Address, min, max) = await portBruteForcer.PredictMinMaxPortsAsync(
            cancellationToken
        );

        var packetEncryptor = new PacketEncryptor();
        var myInfo = new JoinCallRequest(
            min,
            max,
            myIPv4Address.ToString(),
            packetEncryptor.PublicKey
        );

        var connectionInfo = await exchangeConnectionInfo(myInfo);

        var connection = await portBruteForcer.EstablishConnectionAsync(
            IPAddress.Parse(connectionInfo.IPAddress),
            connectionInfo.MinPort,
            connectionInfo.MaxPort,
            cancellationToken
        );

        packetEncryptor.SetAesIV(connectionInfo.IV!);
        packetEncryptor.SetOtherPartyPublicKey(connectionInfo.PublicKey);

        connection = new OutOfOrderDropper(connection);
        connection = new EncryptedConnection(connection, packetEncryptor);
        connection = new CallHanger(connection);

        return new PacketStream(connection);
    }
}