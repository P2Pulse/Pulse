using System.Net.Sockets;

namespace Pulse.Core.Connections;

internal class UdpBiDirectionalStream : IBiDirectionalStream
{
    private readonly NetworkStream _networkStream;
    public UdpBiDirectionalStream(Socket socket)
    {
        _networkStream = new NetworkStream(socket);
    }
    
    public Stream Input => _networkStream;
    public Stream Output => _networkStream;
    
    public ValueTask DisposeAsync()
    {
        return _networkStream.DisposeAsync();
    }
}