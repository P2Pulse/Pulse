using System.Net.Sockets;

namespace Pulse.Core.Connections;

internal class UdpBiDirectionalStream : IBiDirectionalStream
{
    private readonly UdpClient sender;
    private readonly UdpClient receiver;

    public UdpBiDirectionalStream(Socket socket)
    {
        var socketLocalEndPoint = socket.LocalEndPoint;
        var socketRemoteEndPoint = socket.RemoteEndPoint;
        socket.Dispose();
        
        sender = new UdpClient
        {
            ExclusiveAddressUse = false
        };
        
        sender.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        sender.Client.Bind(socketLocalEndPoint!);
        sender.Client.Connect(socketRemoteEndPoint!);
        
        receiver = new UdpClient
        {
            ExclusiveAddressUse = false
        };
        receiver.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        receiver.Client.Bind(socketLocalEndPoint!);
        
        sender.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        
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