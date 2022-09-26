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
        for (var i = minPort - 75; i <= minPort + 75; i += 75)
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
        // var s2 = "stun.jumblo.com";
        // var s2 = "stun.voipbuster.com";
        // var s4 = "stun.voipstunt.com";
        // var s5 = "stun.ekiga.net";
        // var s6 = "stun.ideasip.com";
        // var s7 = "stun.voiparound.com";
        // var s8 = "stun.voipbusterpro.com";

        var random = new Random();
        var queriesAmount = random.Next(2, 3);

        try
        {
            var stunQueriesS1 = Enumerable.Range(0, queriesAmount)
                .Select(i => GetPublicIPEndpointAsync(receiver.Client, s1, cancellationToken));

            // var stunQueriesS2 = Enumerable.Range(0, queriesAmount)
            // .Select(i => GetPublicIPEndpointAsync(receivers[0].Client, s2, cancellationToken));
            Console.WriteLine(4.5);
            var responses = await Task.WhenAll(stunQueriesS1);
            Console.WriteLine(5);
            var ports = responses.Select(r => r.Port).ToList();
            Console.WriteLine(string.Join(",", ports));
            var max = ports.Max();
            var min = ports.Min();
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