using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using STUN;

namespace Pulse.Core.Connections;

internal class PortBruteForceNatTraversal
{
    private readonly List<UdpClient> receivers;
    private readonly List<UdpClient> senders;

    public PortBruteForceNatTraversal()
    {
        receivers = Enumerable.Repeat(0, count: 200).Select(_ =>
        {
            var udpClient = new UdpClient
            {
                ExclusiveAddressUse = false
            };
            udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, 0));
            return udpClient;
        }).ToList();

        senders = receivers.Select(r =>
        {
            var sender = new UdpClient
            {
                ExclusiveAddressUse = false
            };
            sender.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            sender.Client.Bind(r.Client.LocalEndPoint!);
            return sender;
        }).ToList();

        Console.WriteLine("Finished initializing all the clients");
    }

    public async Task<IConnection> EstablishConnectionAsync(IPAddress destination, int minPort, int maxPort,
        CancellationToken cancellationToken = default)
    {
        Console.WriteLine(8);
        IPEndPoint? messageRemoteEndPoint = null;
        var connectionInitiated = false;
        var selectedReceiver = null as UdpClient;
        foreach (var receiver in receivers)
        {
            Console.WriteLine("Starting to listen");
            _ = Task.Run(async () =>
            {
                try
                {
                    var message = await receiver.ReceiveAsync(cancellationToken);
                    Console.WriteLine($"Got a punching message: {Encoding.ASCII.GetString(message.Buffer)}");
                    messageRemoteEndPoint = message.RemoteEndPoint;
                    selectedReceiver = receiver;
                    connectionInitiated = true;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }, cancellationToken);
        }
        
        
        Console.WriteLine("Starting to punch holes");
        
        var message = Encoding.ASCII.GetBytes("Punch!");
        for (var i = 0; i < 10; i++)
        {
            foreach (var sender in senders)
            {
                if (connectionInitiated)
                {
                    sender.Dispose();
                    await selectedReceiver!.Client.ConnectAsync(messageRemoteEndPoint, cancellationToken);
                    for (var j = 0; j < 20; j++)
                    {
                        var datagram = Encoding.ASCII.GetBytes("Knockout");
                        await selectedReceiver.SendAsync(datagram, cancellationToken);
                        Sleep(TimeSpan.FromMilliseconds(10));
                    }
                    
                    return new UdpChannel(selectedReceiver);
                }
                
                var endpoint = new IPEndPoint(destination, minPort + i);
                await sender.SendAsync(message, endpoint, cancellationToken);
                Sleep(TimeSpan.FromMilliseconds(5));
            }

            Console.WriteLine("Loop");
        }
        
        // TODO: dispose all clients...

        throw new Exception("Connection failed:(");
    }

    private static void Sleep(TimeSpan duration)
    {
        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < duration)
            ;
    }

    private static async Task<IPEndPoint> GetPublicIPEndpointAsync(Socket socket, string hostName,
        CancellationToken cancellationToken)
    {
        while (true)
        {
            Console.WriteLine(1);
            var serverIp = (await Dns.GetHostAddressesAsync(hostName, cancellationToken)).First();
            var server = new IPEndPoint(serverIp, 3478);
            Console.WriteLine(2);
            var result = await STUNClient.QueryAsync(socket, server, STUNQueryType.PublicIP);
            Console.WriteLine(3);
            if (result?.PublicEndPoint is not null)
                return result.PublicEndPoint;
            await Task.Delay(50, cancellationToken);
            Console.WriteLine(4);
            Console.WriteLine(hostName);
        }
    }

    public async Task<(IPAddress, int minPort, int maxPort)> PredictMinMaxPortsAsync(
        CancellationToken cancellationToken = default)
    {
        Console.WriteLine("Getting ports");
        var s1 = "stun.nventure.com";
        var s2 = "stun.qq.com";

        var random = new Random();
        var queriesAmount = random.Next(2, 4);

        try
        {
            var stunQueriesS1 = Enumerable.Range(0, queriesAmount)
                .Select(i => GetPublicIPEndpointAsync(receivers[0].Client, s1, cancellationToken));
        
            queriesAmount = random.Next(2, 4);
        
            var stunQueriesS2 = Enumerable.Range(0, queriesAmount)
                .Select(i => GetPublicIPEndpointAsync(receivers[0].Client, s2, cancellationToken));
            Console.WriteLine(4.5);
            var responses = await Task.WhenAll(stunQueriesS1.Concat(stunQueriesS2));
            Console.WriteLine(5);
            var ports = responses.Select(r => r.Port).ToList();
            Console.WriteLine(string.Join(",", ports));
            var max = ports.Max();
            var min = ports.Min() + 50;
            max = 0; // TODO: remove max
            var myIp4Address = responses.First().Address;
            Console.WriteLine(6);
            return (myIp4Address, min, max);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}