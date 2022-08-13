using System.Net.Sockets;

namespace Pulse.Core.Connections;

internal class UdpBiDirectionalStream : IBiDirectionalStream
{
    private readonly NetworkStream networkStream;
    public UdpBiDirectionalStream(Socket socket)
    {
        networkStream = new NetworkStream(socket);
    }
    
    public Stream Input => networkStream;
    public Stream Output => networkStream;
    
    public ValueTask DisposeAsync()
    {
        return networkStream.DisposeAsync();
    }
}