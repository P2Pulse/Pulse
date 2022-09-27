using System.Net;
using System.Net.Sockets;
using System.Text;

Console.WriteLine("Hello World");
try
{
    File.Delete("output.txt");
}
catch (Exception e)
{
    // ignored
}

for (var i = 0; i < 1500; i++)
{
    var udpClient = new UdpClient();
    udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, i + 14000));
    var x = i;
    _ = Task.Run(async () =>
    {
        Console.WriteLine("starting " + x);
        while (true)
        {
            try
            {
                var message = await udpClient.ReceiveAsync();
                var contents = $"{udpClient.Client.LocalEndPoint} | {message.RemoteEndPoint.Port}";
                Console.WriteLine(contents);
                File.AppendAllText("output.txt", contents + "\n");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    });
}

Console.WriteLine("Waiting");
var mainUdpClient = new UdpClient();
mainUdpClient.Client.Bind(new IPEndPoint(IPAddress.Any, 15900));

Console.WriteLine("starting main");
try
{
    var message = await mainUdpClient.ReceiveAsync();
    File.AppendAllText("output.txt",
        $"Got a punching message: {Encoding.ASCII.GetString(message.Buffer)} from {message.RemoteEndPoint.Port}");
}
catch (Exception e)
{
    Console.WriteLine(e);
}

Console.WriteLine("Got a message");
await Task.Delay(-1);