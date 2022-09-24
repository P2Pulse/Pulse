using System.Net;
using System.Net.Sockets;
using System.Text;
using STUN;

namespace Pulse.Core.Connections;

internal class PortBruteForceNatTraversal
{
    private readonly UdpClient receiver;

    public PortBruteForceNatTraversal()
    {
        receiver = new UdpClient
        {
            ExclusiveAddressUse = false
        };
        receiver.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        receiver.Client.Bind(new IPEndPoint(IPAddress.Any, 0));
    }

    public async Task<IConnection> EstablishConnectionAsync(IPAddress destination, int minPort, int maxPort,
        CancellationToken cancellationToken = default)
    {
        IPEndPoint? messageRemoteEndPoint = null;
        var connectionInitiated = false;
        _ = Task.Run(async () =>
        {
            try
            {
                var message = await receiver.ReceiveAsync(cancellationToken);
                Console.WriteLine($"Got a punching message: {Encoding.ASCII.GetString(message.Buffer)}");
                messageRemoteEndPoint = message.RemoteEndPoint;
                connectionInitiated = true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }, cancellationToken);

        using var sender = new UdpClient();
        sender.ExclusiveAddressUse = false;
        sender.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        sender.Client.Bind(receiver.Client.LocalEndPoint!);
        var port = ((IPEndPoint)receiver.Client.LocalEndPoint!).Port;
        Console.WriteLine("Starting to punch holes");

        short ttl = 4;
        while (true)
        {
            sender.Ttl = ttl;
            ttl++;
            ttl++;
            ttl++;
            
            var message = Encoding.ASCII.GetBytes("Punch!");
            for (var destinationPort = minPort; destinationPort <= maxPort; destinationPort++)
            {
                if (connectionInitiated)
                {
                    sender.Dispose();
                    await receiver.Client.ConnectAsync(messageRemoteEndPoint, cancellationToken);
                    var datagram = Encoding.ASCII.GetBytes("Knockout");
                    await receiver.SendAsync(datagram, cancellationToken);
                    return new UdpChannel(receiver);
                }

                var endpoint = new IPEndPoint(destination, destinationPort);
                await sender.SendAsync(message, endpoint, cancellationToken);
                if (destinationPort % 5 is 0)
                    await Task.Delay(5, cancellationToken);
            }
            
            Console.WriteLine("loop");
        }
    }

    private static async Task<IPEndPoint> GetPublicIPEndpointAsync(Socket socket, string hostName,
        CancellationToken cancellationToken)
    {
        while (true)
        {
            var serverIp = (await Dns.GetHostAddressesAsync(hostName, cancellationToken)).First();
            var server = new IPEndPoint(serverIp, 3478);
            var result = await STUNClient.QueryAsync(socket, server, STUNQueryType.PublicIP);
            if (result?.PublicEndPoint is not null)
                return result.PublicEndPoint;
            await Task.Delay(50, cancellationToken);
        }
    }

    public async Task<(IPAddress, int minPort, int maxPort)> PredictMinMaxPortsAsync(
        CancellationToken cancellationToken = default)
    {
        var s1 = "stun.schlund.de";
        var s2 = "stun.jumblo.com";
        var stunQueriesS1 = Enumerable.Range(0, 2)
            .Select(i => GetPublicIPEndpointAsync(receiver.Client, s1, cancellationToken));

        var stunQueriesS2 = Enumerable.Range(0, 2)
            .Select(i => GetPublicIPEndpointAsync(receiver.Client, s2, cancellationToken));

        var responses = await Task.WhenAll(stunQueriesS1.Concat(stunQueriesS2));
        var ports = responses.Select(r => r.Port).ToList();
        var max = ports.Max();
        var min = ports.Min();
        min = Math.Max(min - 75, 1);
        max = Math.Min(max + 75, ushort.MaxValue);
        var myIp4Address = responses.First().Address;
        return (myIp4Address, min, max);
    }
}