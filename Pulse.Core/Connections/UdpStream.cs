using System.Net.Sockets;

namespace Pulse.Core.Connections;

internal class UdpStream : Stream
{
    private readonly UdpClient _udpClient;
    private const string UseAsyncVersionMessage = "Use async version";

    public UdpStream(UdpClient udpClient, bool isReader)
    {
        _udpClient = udpClient;
        CanRead = isReader;
        CanWrite = !isReader;
    }

    public override bool CanRead { get; }
    public override bool CanSeek => false;
    public override bool CanWrite { get; }
    public override long Length => throw new NotSupportedException();
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        if (!CanRead) 
            throw new NotSupportedException();
        
        var incomingPacket = await _udpClient.ReceiveAsync(cancellationToken);
        var bytesRead = Math.Min(count, incomingPacket.Buffer.Length);
        Array.Copy(incomingPacket.Buffer, sourceIndex: 0, buffer, offset, bytesRead);
        return bytesRead;
    }

    public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        if (!CanWrite) 
            throw new NotSupportedException();
        
        await _udpClient.SendAsync(buffer[offset..(offset + count)], count);
    }

    public override void Flush()
    {
        throw new NotSupportedException(UseAsyncVersionMessage);
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException(UseAsyncVersionMessage);
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException(UseAsyncVersionMessage);
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException(UseAsyncVersionMessage);
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException(UseAsyncVersionMessage);
    }
}