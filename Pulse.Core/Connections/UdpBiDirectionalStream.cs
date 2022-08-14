using System.Net.Sockets;

namespace Pulse.Core.Connections;

internal class UdpBiDirectionalStream : IBiDirectionalStream
{
    private readonly UdpClient sender;
    private readonly UdpClient receiver;
    public UdpBiDirectionalStream(Socket socket)
    {
        sender = new UdpClient
        {
            Client = socket
        };
        receiver = new UdpClient
        {
            Client = socket
        };
        Input = new UdpStream(receiver, isReader: true);
        Output = new UdpStream(sender, isReader: false);
    }
    
    public Stream Input { get; }
    public Stream Output { get; }
    
    public ValueTask DisposeAsync()
    {
        sender.Dispose();
        receiver.Dispose();
        
        return ValueTask.CompletedTask;
    }
}