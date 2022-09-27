using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using STUN;

namespace Pulse.Core.Connections;

internal class PortBruteForceNatTraversal : IAsyncDisposable
{
    private readonly List<UdpClient> receivers;
    private List<UdpClient> senders;

    public PortBruteForceNatTraversal()
    {
        var firstEndpoint = null as IPEndPoint;
        receivers = Enumerable.Range(0, count: 200).Select(i =>
        {
            var udpClient = new UdpClient();

            if (firstEndpoint is null)
            {
                do
                {
                    udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, 0));
                    firstEndpoint = udpClient.Client.LocalEndPoint as IPEndPoint;
                    Console.WriteLine($"Trying to obtain first endpoint");
                } while (firstEndpoint.Port > 65000);
            }
            else
            {
                try
                {
                    udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, firstEndpoint.Port + i)); // TODO: can overflow
                }
                catch (SocketException)
                {
                    Console.WriteLine("fuck");
                    udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, 0)); // TODO: Fix this
                }
            }

            return udpClient;
        }).ToList();

        Console.WriteLine("Finished initializing all the clients");
    }

    public async Task<IConnection> EstablishConnectionAsync(IPAddress destination, int minPort, int maxPort,
        CancellationToken cancellationToken = default)
    {
        /*minPort = 15000;
        destination = IPAddress.Parse("18.196.107.59");*/
        senders = receivers.Select(r =>
        {
            var sender = new UdpClient
            {
                Client = r.Client
            };
            return sender;
        }).ToList();

        IPEndPoint? messageRemoteEndPoint = null;
        var connectionInitiated = false;
        var selectedReceiver = null as UdpClient;
        foreach (var receiver in receivers)
        {
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
        for (var i = 0; i < 5; i++)
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

                    receivers.Remove(selectedReceiver);
                    return new UdpChannel(selectedReceiver);
                }

                for (var j = 0; j < 10; j++)
                {
                    var sign = -1;
                    sign = (int)Math.Pow(sign, j);
                    var endpoint = new IPEndPoint(destination, minPort + 3 * j * sign);
                    await sender.SendAsync(message, endpoint, cancellationToken);
                    Sleep(TimeSpan.FromMilliseconds(2.5));
                }
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
        var serverIp = (await Dns.GetHostAddressesAsync(hostName, cancellationToken)).First();
        var server = new IPEndPoint(serverIp, 3478);
        // Console.WriteLine(2);
        var result = await STUNClient.QueryAsync(socket, server, STUNQueryType.PublicIP)
            .WaitAsync(TimeSpan.FromSeconds(3), cancellationToken);
        // Console.WriteLine("stun error if exists: " + result.QueryError);
        // Console.WriteLine(3);
        if (result?.PublicEndPoint is not null)
        {
            // Console.WriteLine("Successfully got public endpoint");
            return result.PublicEndPoint;
        }

        throw new Exception("Failed to get response from STUN server");
    }

    public async Task<(IPAddress, int minPort, int maxPort)> PredictMinMaxPortsAsync(
        CancellationToken cancellationToken = default)
    {
        Console.WriteLine("Getting ports");
        var stunServers = new[]
        {
            "iphone-stun.strato-iphone.de", "stun.12connect.com", "stun.12voip.com", "stun.1und1.de",
            "stun.acrobits.cz", "stun.actionvoip.com", "stun.aeta-audio.com", "stun.aeta.com", "stun.altar.com.pl",
            "stun.annatel.net", "stun.avigora.fr", "stun.bluesip.net", "stun.cablenet-as.net",
            "stun.callromania.ro", "stun.callwithus.com", "stun.cheapvoip.com", "stun.commpeak.com", "stun.cope.es",
            "stun.counterpath.com", "stun.counterpath.net", "stun.dcalling.de", "stun.demos.ru", "stun.dus.net",
            "stun.easycall.pl", "stun.easyvoip.com", "stun.ekiga.net", "stun.epygi.com", "stun.etoilediese.fr",
            "stun.freecall.com", "stun.freeswitch.org", "stun.freevoipdeal.com", "stun.gmx.de", "stun.gmx.net",
            "stun.halonet.pl", "stun.hoiio.com", "stun.hosteurope.de", "stun.infra.net", "stun.internetcalls.com",
            "stun.intervoip.com", "stun.ippi.fr", "stun.ipshka.com", "stun.it1.hr", "stun.ivao.aero",
            "stun.jumblo.com", "stun.justvoip.com", "stun.liveo.fr", "stun.lowratevoip.com", "stun.lundimatin.fr",
            "stun.mit.de", "stun.miwifi.com"
        };

        try
        {
            var responses = await Task.WhenAll(stunServers.Select(async (s, index) =>
            {
                try
                {
                    return await GetPublicIPEndpointAsync(receivers[index].Client, s, cancellationToken);
                }
                catch
                {
                    Console.WriteLine("Failed to get response from " + s);
                    return null;
                }
            }));

            var minPort = (int)responses.Where(r => r is not null).Select(r => r!.Port).Average();

            return (responses.First(r => r is not null)!.Address, minPort, minPort);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public ValueTask DisposeAsync()
    {
        foreach (var udpClient in receivers.Concat(senders)) 
            udpClient.Dispose();
        
        return ValueTask.CompletedTask;
    }
}