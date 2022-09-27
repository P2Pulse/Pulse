using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using STUN;

namespace Pulse.Core.Connections;

internal class PortBruteForceNatTraversal
{
    private readonly List<UdpClient> receivers;
    private List<UdpClient> senders;
    private readonly UdpClient receiver; // TODO: fucking solve this shit

    public PortBruteForceNatTraversal()
    {
        var firstEndpoint = null as IPEndPoint;
        receivers = Enumerable.Repeat(0, count: 200).Select(i =>
        {
            var udpClient = new UdpClient();

            if (firstEndpoint is null)
            {
                udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, 0));
                firstEndpoint = udpClient.Client.LocalEndPoint as IPEndPoint;
            }
            else
            {
                try
                {
                    udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, firstEndpoint.Port + i)); // TODO: can overflow
                }
                catch (SocketException)
                {
                    udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, 0)); // TODO: Fix this
                }
            }

            return udpClient;
        }).ToList();

        receiver = receivers[0];
        Console.WriteLine("Finished initializing all the clients");
    }

    public async Task<IConnection> EstablishConnectionAsync(IPAddress destination, int minPort, int maxPort,
        CancellationToken cancellationToken = default)
    {
        senders = receivers.Select(r =>
        {
            var sender = new UdpClient
            {
                Client = r.Client
            };
            return sender;
        }).ToList();

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

                var endpoint = new IPEndPoint(destination, minPort);
                await sender.SendAsync(message, endpoint, cancellationToken);
                Sleep(TimeSpan.FromMilliseconds(5));
            }

            Console.WriteLine("Loop");
        }

        Sleep(TimeSpan.FromMilliseconds(500));
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
            Console.WriteLine("stun error if exists: " + result.QueryError);
            Console.WriteLine(3);
            if (result?.PublicEndPoint is not null)
            {
                Console.WriteLine("Successfully got public endpoint");
                return result.PublicEndPoint;
            }

            await Task.Delay(50, cancellationToken);
            Console.WriteLine(4);
            Console.WriteLine(hostName);
        }
    }

    public async Task<(IPAddress, int minPort, int maxPort)> PredictMinMaxPortsAsync(
        CancellationToken cancellationToken = default)
    {
        Console.WriteLine("Getting ports");
        var s1 = "stun.schlund.de";
        var s2 = "stun.voip.blackberry.com";

        try
        {
            Console.WriteLine(4.4);
            var response1 = await GetPublicIPEndpointAsync(receiver.Client, s1, cancellationToken);
            var firstPort = response1.Port;
            Console.WriteLine(4.5);
            var response2 = await GetPublicIPEndpointAsync(receiver.Client, s2, cancellationToken);
            var secondPort = response2.Port;
            Console.WriteLine(4.6);

            int minPort;
            if (firstPort < secondPort)
                minPort = secondPort + 75;
            else if (secondPort < firstPort)
                minPort = secondPort - 75;
            else
                minPort = firstPort;

            var myIp4Address = response1.Address;
            Console.WriteLine(6);
            return (myIp4Address, minPort, minPort);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}