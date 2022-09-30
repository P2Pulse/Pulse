using System.Diagnostics;
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
        receiver = new UdpClient();
        receiver.Client.Bind(new IPEndPoint(IPAddress.Any, 0));
    }

    public async Task<IConnection> EstablishConnectionAsync(IPAddress destination, int minPort, int maxPort,
        CancellationToken cancellationToken = default)
    {
        // destination = IPAddress.Parse("18.198.4.113");
        // maxPort = 15000 + (maxPort - minPort);
        // minPort = 15000;
        IPEndPoint? messageRemoteEndPoint = null;
        var connectionInitiated = false;
        _ = Task.Run(async () =>
        {
            try
            {
                var message = await receiver.ReceiveAsync(cancellationToken).ConfigureAwait(false);
                Console.WriteLine($"Got a punching message: {Encoding.ASCII.GetString(message.Buffer)}");
                messageRemoteEndPoint = message.RemoteEndPoint;
                connectionInitiated = true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }, cancellationToken);

        using var sender = new UdpClient
        {
            Client = receiver.Client
        };
        Console.WriteLine("Starting to punch holes");

        for (var k = 0; k < 4; k++)
        {
            if (k != 0)
            {
                Console.WriteLine("Waiting a little before the next rounds to let the other party punch the NAT.");
                await Task.Delay(1250 * k, cancellationToken).ConfigureAwait(false);
            }

            var message = Encoding.ASCII.GetBytes("Punch!");
            for (var destinationPort = minPort; destinationPort <= maxPort; destinationPort++)
            {
                if (connectionInitiated)
                {
                    sender.Client =
                        null; // Prevents closing the socket when disposing the client because we are using the same socket for both sending and receiving
                    await receiver.Client.ConnectAsync(messageRemoteEndPoint, cancellationToken).ConfigureAwait(false);
                    for (var i = 0; i < 20; i++)
                    {
                        var datagram = Encoding.ASCII.GetBytes("Knockout");
                        await receiver.SendAsync(datagram, cancellationToken).ConfigureAwait(false);
                        Sleep(TimeSpan.FromMilliseconds(2.5));
                    }

                    return new UdpChannel(receiver);
                }

                var endpoint = new IPEndPoint(destination, destinationPort);
                await sender.SendAsync(message, endpoint, cancellationToken).ConfigureAwait(false);
                Sleep(TimeSpan.FromMilliseconds(7));
            }

            Console.WriteLine("loop");
        }

        throw new Exception("Could not establish connection ):");
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
            var serverIp = (await Dns.GetHostAddressesAsync(hostName, cancellationToken).ConfigureAwait(false)).First();
            var server = new IPEndPoint(serverIp, 3478);
            var result = await STUNClient.QueryAsync(socket, server, STUNQueryType.PublicIP).ConfigureAwait(false);
            if (result?.PublicEndPoint is not null)
                return result.PublicEndPoint;
            await Task.Delay(50, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<(IPAddress, int minPort, int maxPort)> PredictMinMaxPortsAsync(
        CancellationToken cancellationToken = default)
    {
        const string s1 = "stun.schlund.de";
        const string s2 = "stun.jumblo.com";

        var ipEndPoints = new List<IPEndPoint>
        {
            await GetPublicIPEndpointAsync(receiver.Client, s1, cancellationToken).ConfigureAwait(false),
            await GetPublicIPEndpointAsync(receiver.Client, s2, cancellationToken).ConfigureAwait(false)
        };

        var ports = ipEndPoints.Select(i => i.Port).ToList();

        Console.WriteLine(string.Join(",", ipEndPoints));
        var myIp4Address = ipEndPoints.First().Address;

        var max = ports.Max();
        var min = ports.Min();
        if (min == max) // in case it's not a symmetric NAT
            return (myIp4Address, min, max);

        min = Math.Max(min - 375, 1);
        max = Math.Min(max + 375, ushort.MaxValue);
        return (myIp4Address, min, max);
    }
}