using System.Net;
using System.Net.Sockets;
using System.Text;
using STUN;

namespace Pulse.Core.Connections;

public class UdpMultiHolePunching : IConnectionEstablishmentStrategy
{
    public async Task<IConnection> EstablishConnectionAsync(IPAddress destination, 
        CancellationToken cancellationToken = default)
    {
        Console.WriteLine("Am I an Echo Client or an Echo Server? (c/s)");
        var hostType = Console.ReadLine();
        
        var predictedNextPort = await PredictNextPortAsync(cancellationToken);
        if (hostType is "c")
        {
            Console.WriteLine("Tell the echo server my port is around " + predictedNextPort);
            Console.Write("What is the server's port? ");
            var serverPort = int.Parse(Console.ReadLine()!);
            for (int i = 0; i < 500; i++)
            {
                var customCancellationTokenSource = new CancellationTokenSource();
                await Parallel.ForEachAsync(Enumerable.Repeat(0, 1000), customCancellationTokenSource.Token, async (_, ct) =>
                {
                    var receiver = new UdpClient
                    {
                        ExclusiveAddressUse = false
                    };
                    receiver.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    receiver.Client.Bind(new IPEndPoint(IPAddress.Any, 0));
                    ThreadPool.QueueUserWorkItem(async _ =>
                    {
                        using (receiver)
                        {
                            while (!customCancellationTokenSource.Token.IsCancellationRequested)
                            {
                                UdpReceiveResult message;
                                try
                                {
                                    message = await receiver.ReceiveAsync(customCancellationTokenSource.Token);
                                }
                                catch (Exception e)
                                {
                                    // Console.WriteLine(e);
                                    // throw;
                                    return;
                                }
                                Console.WriteLine($"Received {message.Buffer.Length} bytes from {message.RemoteEndPoint}:");
                                var messageContent = Encoding.ASCII.GetString(message.Buffer);
                                Console.WriteLine(messageContent);
                            }

                            Console.WriteLine("juyhtgf");
                        }
                    });
                
                    using var sender = new UdpClient();
                    sender.ExclusiveAddressUse = false;
                    sender.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    sender.Client.Bind(receiver.Client.LocalEndPoint);
                    var message = Encoding.ASCII.GetBytes("Do you hear me?");
                    try
                    {
                        await sender.SendAsync(message, new IPEndPoint(destination, serverPort),
                            customCancellationTokenSource.Token);
                    }
                    catch
                    {
                        
                    }
                });
                await Task.Delay(1000);
                customCancellationTokenSource.Cancel();
                await Task.Delay(100);
                serverPort += i;
                Console.WriteLine(i);
                serverPort %= ushort.MaxValue;
            }
        }
        else
        {
            Console.WriteLine("What is the client's port? ");
            var clientPort = int.Parse(Console.ReadLine()!);
            // Hole punching
            await Parallel.ForEachAsync(Enumerable.Repeat(0, 1000),new ParallelOptions{MaxDegreeOfParallelism = 1000}, async (_, ct) =>
            {
                var udpClient = new UdpClient
                {
                    ExclusiveAddressUse = false
                };
                udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, 0));
                Console.WriteLine(udpClient.Client.LocalEndPoint);
                ThreadPool.QueueUserWorkItem(async _ =>
                {
                    using (udpClient)
                    {
                        while (!cancellationToken.IsCancellationRequested)
                        {
                            UdpReceiveResult message;
                            try
                            {
                                message = await udpClient.ReceiveAsync(cancellationToken);
                                var response = Encoding.ASCII.GetBytes("I hear you!");
                                await udpClient.SendAsync(response, message.RemoteEndPoint, ct);
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e.StackTrace);
                                throw;
                            }
                            Console.WriteLine($"Received {message.Buffer.Length} bytes from {message.RemoteEndPoint}:");
                            var messageContent = Encoding.ASCII.GetString(message.Buffer);
                            Console.WriteLine(messageContent);
                        }
                    }
                });
                
                using var sender = new UdpClient();
                sender.ExclusiveAddressUse = false;
                sender.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                sender.Client.Bind(udpClient.Client.LocalEndPoint);
                sender.Ttl = 5;  //
                var punch = Encoding.ASCII.GetBytes("In your face, NAT!");
                while (true)
                {
                    await sender.SendAsync(punch,new IPEndPoint(destination, clientPort), ct);
                    await Task.Delay(1000, ct);
                }
            });
            Console.WriteLine("Tell the client my port is around " + predictedNextPort);
        }
        

        return null;
    }

    private static async Task<int> PredictNextPortAsync(CancellationToken cancellationToken)
    {
        var result = 0;
        for (int i = 0; i < 5; i++)
        {
            var s1 = "stun.schlund.de";
            var s2 = "stun.jumblo.com";
            var s1Response = await GetPublicIPEndpointAsync(s1, cancellationToken);
            var s2Response = await GetPublicIPEndpointAsync(s2, cancellationToken);

            Console.WriteLine($"{s1} is {s1Response.Port}");
            Console.WriteLine($"{s2} is {s2Response.Port}");
        
            result = 2 * s2Response.Port - s1Response.Port;
        }

        return result;
    }

    private static async Task<IPEndPoint> GetPublicIPEndpointAsync(string hostName,
        CancellationToken cancellationToken)
    {
        var serverIp = (await Dns.GetHostAddressesAsync(hostName, cancellationToken)).First();
        var server = new IPEndPoint(serverIp, 3478);
        var result = await STUNClient.QueryAsync(server, STUNQueryType.PublicIP, closeSocket: true);
        return result.PublicEndPoint;
    }
}