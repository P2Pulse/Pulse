using System.Threading.Channels;
using Pulse.Core.AudioStreaming;
using Pulse.Core.Connections;
using System;

namespace Pulse.Core.Tests.AudioStreaming;

public class PacketStreamTests
{
    private readonly PacketStream packetStream;
    private readonly IConnection connection;
    private readonly Channel<Packet> channel = Channel.CreateUnbounded<Packet>();
    private readonly Random random = new(Seed: 123456);

    public PacketStreamTests()
    {
        connection = Substitute.For<IConnection>();
        connection.IncomingAudio.Returns(channel.Reader);
        packetStream = new PacketStream(connection);
    }

    [Fact]
    public async Task ReadAsync_BufferTooSmall_ShouldSaveLeftover()
    {
        var audio = GetFakeAudio(100);
        var buffer1 = new byte[50];
        var buffer2 = new byte[50];
        await channel.Writer.WriteAsync(audio);
        
        var bytesRead1 = await packetStream.ReadAsync(buffer1);
        var bytesRead2 = await packetStream.ReadAsync(buffer2);

        bytesRead1.Should().Be(buffer1.Length);
        bytesRead2.Should().Be(buffer2.Length);
        buffer1.Should().Equal(audio.Content[..buffer1.Length].ToArray());
        buffer2.Should().Equal(audio.Content[buffer1.Length..].ToArray());
    }
    
    [Fact]
    public async Task ReadAsync_PacketTooSmall_ShouldWaitForAnotherPacket()
    {
        var audio1 = GetFakeAudio(50);
        var audio2 = GetFakeAudio(50);
        var buffer = new byte[100];
        await channel.Writer.WriteAsync(audio1);
        await channel.Writer.WriteAsync(audio2);
        
        var bytesRead = await packetStream.ReadAsync(buffer);

        bytesRead.Should().Be(buffer.Length);
        buffer.Should().Equal(audio1.Content.ToArray().Concat(audio2.Content.ToArray()));
    }

    [Fact]
    public async Task ReadAsync_LastPacket_ShouldReturnWithoutFillingTheWholeBuffer()
    {
        var audio = GetFakeAudio(50);
        await channel.Writer.WriteAsync(audio);
        channel.Writer.Complete();
        var buffer = new byte[100];

        var bytesRead = await packetStream.ReadAsync(buffer);

        bytesRead.Should().Be(audio.Content.Length);
        buffer[..audio.Content.Length].Should().Equal(audio.Content.ToArray());
        buffer[audio.Content.Length..].Should().OnlyContain(b => b == 0);
    }

    private Packet GetFakeAudio(int bytes)
    {
        var audio = new byte[bytes];
        random.NextBytes(audio);
        return new Packet(audio);
    }
}