using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Pulse.Core.Connections;

public static class YotamGarua
{
    public static async Task SendMessageAsync(this IPAddress address, UdpClient client, int destinationPort, string message = "Ata debil")
    {
        var data = Encoding.ASCII.GetBytes(message);
        await Task.Delay(TimeSpan.FromMilliseconds(0.5));
        await client.SendAsync(data, data.Length, new IPEndPoint(address, destinationPort));
    }
}