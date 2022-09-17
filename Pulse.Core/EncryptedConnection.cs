using System.Threading.Channels;
using Pulse.Core.Connections;

namespace Pulse.Core;

internal class EncryptedConnection : IConnection
{
    private readonly IConnection actualConnection;
    private readonly PacketEncryptor encryptor;

    public EncryptedConnection(IConnection actualConnection, PacketEncryptor encryptor)
    {
        if (!encryptor.Ready) throw new InvalidOperationException("Encryptor is not properly configured!");

        this.actualConnection = actualConnection;
        this.encryptor = encryptor;
        IncomingAudio = new CryptoPacketChannelReader(
            this.actualConnection.IncomingAudio,
            encryptor
        );
    }

    public ChannelReader<Packet> IncomingAudio { get; }

    public async Task SendPacketAsync(Packet packet, CancellationToken cancellationToken)
    {
        var encryptedPacket = await encryptor.EncryptAsync(packet);
        await actualConnection.SendPacketAsync(encryptedPacket, cancellationToken);
    }

    private class CryptoPacketChannelReader : ChannelReader<Packet>
    {
        private readonly ChannelReader<Packet> actualChannelReader;
        private readonly PacketEncryptor encryptor;

        public CryptoPacketChannelReader(
            ChannelReader<Packet> actualChannelReader,
            PacketEncryptor encryptor
        )
        {
            this.actualChannelReader = actualChannelReader;
            this.encryptor = encryptor;
        }

        public override bool TryRead(out Packet item)
        {
            if (!actualChannelReader.TryRead(out item))
                return false;

            item = encryptor.DecryptAsync(item).GetAwaiter()
                .GetResult(); // Todo make the encrypt and decrypt methods sync
            return true;
        }

        public override ValueTask<bool> WaitToReadAsync(CancellationToken cancellationToken = default)
        {
            return actualChannelReader.WaitToReadAsync(cancellationToken);
        }
    }

    public ValueTask DisposeAsync()
    {
        return actualConnection.DisposeAsync();
    }
}