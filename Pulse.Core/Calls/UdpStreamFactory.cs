using System.Net;
using System.Text;
using Pulse.Core.AudioStreaming;
using Pulse.Core.Connections;
using Pulse.Server.Contracts;

namespace Pulse.Core.Calls;

internal class UdpStreamFactory
{
    public async Task<EncryptedStream> ConnectAsync(
        Func<JoinCallRequest, Task<ConnectionDetails>> exchangeConnectionInfo,
        CancellationToken cancellationToken = default
    )
    {
        var portBruteForcer = new PortBruteForceNatTraversal();
        var (myIPv4Address, min, max) = await portBruteForcer.PredictMinMaxPortsAsync(
            cancellationToken
        ).ConfigureAwait(false);

        var packetEncryptor = new PacketEncryptor();
        var myInfo = new JoinCallRequest(
            min,
            max,
            myIPv4Address.ToString(),
            packetEncryptor.PublicKey
        );

        var connectionInfo = await exchangeConnectionInfo(myInfo).ConfigureAwait(false);

        Console.WriteLine("Connection info:");
        Console.WriteLine(connectionInfo);

        var connection = await portBruteForcer.EstablishConnectionAsync(
            IPAddress.Parse(connectionInfo.IPAddress),
            connectionInfo.MinPort,
            connectionInfo.MaxPort,
            cancellationToken
        ).ConfigureAwait(false);

        packetEncryptor.SetAesIV(connectionInfo.IV!);
        packetEncryptor.SetOtherPartyPublicKey(connectionInfo.PublicKey);

        var credentialsHash = Encoding.ASCII.GetString(packetEncryptor.CalculateCredentialsHash());
        

        connection = new OutOfOrderDropper(connection);
        connection = new EncryptedConnection(connection, packetEncryptor);
        connection = new CallHanger(connection);

        return new EncryptedStream(new PacketStream(connection), credentialsHash);
    }
}

public record EncryptedStream(Stream Stream, string CredentialsHash);