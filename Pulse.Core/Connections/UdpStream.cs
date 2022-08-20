using System.Collections.Concurrent;
using System.Net.Sockets;

namespace Pulse.Core.Connections;

internal class UdpStream : Stream
{
    private readonly UdpClient _udpClient;
    private const string UseAsyncVersionMessage = "Use async version";
    private readonly ConcurrentQueue<byte[]> packets = new();
    private ArraySegment<byte> unreadPart = ArraySegment<byte>.Empty;


    public UdpStream(UdpClient udpClient, bool isReader)
    {
        _udpClient = udpClient;
        CanRead = isReader;
        CanWrite = !isReader;
        if (isReader)
        {
            ThreadPool.QueueUserWorkItem(async _ =>
            {
                while (true)
                {
                    var packet = await udpClient.ReceiveAsync();
                    packets.Enqueue(packet.Buffer);
                }
            });
        }
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

    private bool TryGetNextPacket(out ArraySegment<byte> packet)
    {
        if (unreadPart.Any())
        {
            packet = unreadPart;
            unreadPart = ArraySegment<byte>.Empty;
            return true;
        }

        byte[]? dequeuedPacket;
        while (!packets.TryDequeue(out dequeuedPacket))
            ;
        
        packet = new ArraySegment<byte>(dequeuedPacket);
        return true;
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        if (!CanRead)
            throw new NotSupportedException();

        var bytesRead = 0;

        while (TryGetNextPacket(out var packet))
        {
            var bytesToRead = Math.Min(count - bytesRead, packet.Count);

            packet[..bytesToRead].CopyTo(buffer, offset + bytesRead);

            if (bytesToRead != packet.Count)
            {
                unreadPart = packet[bytesToRead..];
            }

            bytesRead += bytesToRead;

            if (bytesRead == count)
                return bytesRead;
        }

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