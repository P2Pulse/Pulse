using System.Net;
using System.Net.Sockets;
using System.Text;
using STUN;

namespace Pulse.Core.Connections;

public class PortBruteForceNatTraversal : IConnectionEstablishmentStrategy
{
    public async Task<Socket> EstablishConnectionAsync(IPAddress destination,
        CancellationToken cancellationToken = default)
    {
        var receiver = new UdpClient
        {
            ExclusiveAddressUse = false
        };
        receiver.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        receiver.Client.Bind(new IPEndPoint(IPAddress.Any, 0));

        Console.WriteLine(await PredictMinMaxPortsAsync(receiver.Client, cancellationToken));

        Console.WriteLine("what is minimum port of the other person?: ");
        var minPort = int.Parse(Console.ReadLine()!);
        Console.WriteLine("what is maximum port of the other person?: ");
        var maxPort = minPort + 200;
        Console.WriteLine("Starting");
        
        var connectionInitiated = false;
        ThreadPool.QueueUserWorkItem(async _ =>
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                UdpReceiveResult message;
                try
                {
                    message = await receiver.ReceiveAsync(cancellationToken);
                    await receiver.Client.ConnectAsync(message.RemoteEndPoint, cancellationToken);
                    connectionInitiated = true;
                }
                catch (Exception e)
                {
                    return;
                }

                Console.WriteLine($"Received {message.Buffer.Length} bytes from {message.RemoteEndPoint}:");
                var messageContent = Encoding.ASCII.GetString(message.Buffer);
                Console.WriteLine(messageContent);
            }
        });

        using var sender = new UdpClient();
        sender.ExclusiveAddressUse = false;
        sender.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        sender.Client.Bind(receiver.Client.LocalEndPoint!);
        var port = ((IPEndPoint)receiver.Client.LocalEndPoint!).Port;

        while (true)
        {
            for (var destinationPort = minPort; destinationPort <= maxPort; destinationPort++)
            {
                if (connectionInitiated)
                {
                    var client = receiver.Client;
                    receiver.Client = null!; // don't dispose the socket
                    return client;
                }
                
                var endpoint = new IPEndPoint(destination, destinationPort);
                var message = Encoding.ASCII.GetBytes($"Hey from {port} sent to {destinationPort}");
                await sender.SendAsync(message, message.Length, endpoint);
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

    private static async Task<(int, int)> PredictMinMaxPortsAsync(Socket socket,
        CancellationToken cancellationToken)
    {
        var s1 = "stun.schlund.de";
        var s2 = "stun.jumblo.com";
        var stunQueriesS1 = Enumerable.Range(0, 2).Select(i => GetPublicIPEndpointAsync(socket, s1, cancellationToken));
        var stunQueriesS2 = Enumerable.Range(0, 2).Select(i => GetPublicIPEndpointAsync(socket, s2, cancellationToken));
        var responses = await Task.WhenAll(stunQueriesS1.Concat(stunQueriesS2));
        var ports = responses.Select(r => r.Port).ToList();
        var max = ports.Max();
        var min = ports.Min();
        min = Math.Max(min - 100, 1);
        max = Math.Min(max + 100, ushort.MaxValue);
        return (min, max);
    }
}