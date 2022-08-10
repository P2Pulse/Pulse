using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Pulse.Core.Connections;

public class PortBruteForceNatTraversal : IConnectionEstablishmentStrategy
{
    private const int SourcePort = 54911;

    public async Task<IConnection> EstablishConnectionAsync(IPAddress destination, 
        CancellationToken cancellationToken = default)
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
                while (!cancellationToken.IsCancellationRequested)
                {
                    UdpReceiveResult message;
                    try
                    {
                        message = await receiver.ReceiveAsync(cancellationToken);
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
        var port = ((IPEndPoint)receiver.Client.LocalEndPoint).Port;
        var message = Encoding.ASCII.GetBytes($"Hello, I'm talking from {port}");
        for (var i = 54448;; i++)
        {
            var endpoint = new IPEndPoint(destination, i % ushort.MaxValue + 1);
            await sender.SendAsync(message, message.Length, endpoint);
            if (i % 10 is 0)
                await Task.Delay(1, cancellationToken);
        }

        return null;
        /*var shouldSend = true;
        var destinationPort = 0;
        using var client = new UdpClient(SourcePort);
        _ = client.ReceiveAsync(cancellationToken)
            .AsTask().ContinueWith(task =>
            {
                Console.WriteLine("hey got message");
                var udpReceiveResult = task.Result;
                destinationPort = udpReceiveResult.RemoteEndPoint.Port;
                Console.WriteLine("Message received: " + Encoding.ASCII.GetString(udpReceiveResult.Buffer));
                shouldSend = false;
            }, cancellationToken);

        for (var i = 0; shouldSend; i++)
        {
            try
            {
                await destination.SendMessageAsync(client, i % ushort.MaxValue);
            }
            catch (Exception e)
            {
            }
        }

        while (true)
        {
            await destination.SendMessageAsync(client, destinationPort, message: "Yayy");
        }
        // TODO - should listen to the UdpPort and wait for an answer from the other person
        return null;*/
    }
}