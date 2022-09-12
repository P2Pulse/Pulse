﻿using System.Threading.Channels;
using Pulse.Core.Connections;

namespace Pulse.Core.AudioStreaming;

internal class PacketStream : Stream
{
    private readonly IConnection connection;
    private ReadOnlyMemory<byte> leftoverAudio;

    public PacketStream(IConnection connection)
    {
        this.connection = connection;
        leftoverAudio = ReadOnlyMemory<byte>.Empty;
    }

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Length => throw new NotSupportedException();
    
    private static NotSupportedException UseAsyncVersionException => new("Avoid using synchronous methods. Use the async version instead.");

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> destination, CancellationToken cancellationToken = new CancellationToken())
    {
        var bytesRead = ReadAudio(source: leftoverAudio, destination);

        while (destination.Length > bytesRead)
        {
            var packet = await GetNextPacketAsync(cancellationToken);
            if (packet is null)
                return bytesRead;
            
            bytesRead += ReadAudio(source: packet.Content, destination[bytesRead..]);
        }

        return destination.Length;
    }

    private int ReadAudio(ReadOnlyMemory<byte> source, Memory<byte> destination)
    {
        var bytesToRead = Math.Min(source.Length, destination.Length);
        source[..bytesToRead].CopyTo(destination);
        leftoverAudio = source[bytesToRead..];
        return bytesToRead;
    }
    
    private async Task<Packet?> GetNextPacketAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await connection.IncomingAudio.ReadAsync(cancellationToken);
        }
        catch (ChannelClosedException)
        {
            return null;
        }
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        return ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
    }

    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = new CancellationToken())
    {
        var packet = new Packet(buffer);
        await connection.SendPacketAsync(packet, cancellationToken);
    }

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        return WriteAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
    }

    public override void Flush()
    {
        throw UseAsyncVersionException;
    }
    
    public override int Read(byte[] buffer, int offset, int count)
    {
        throw UseAsyncVersionException;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException("Seeking is not supported.");
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException("Setting the length is not supported.");
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw UseAsyncVersionException;
    }
}