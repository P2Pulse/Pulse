using System.Security.Cryptography;
using System.Threading.Channels;
using Pulse.Core.Connections;

namespace Pulse.Core;

internal class EncryptedConnection : IConnection
{
    private readonly IConnection actualConnection;
    private readonly PacketEncryptor encryptor;
    public ChannelReader<Packet> IncomingAudio { get; }

    public EncryptedConnection(IConnection actualConnection, PacketEncryptor encryptor)
    {
        if (!encryptor.IsSharedKeySet)
        {
            throw new InvalidOperationException("Shared key is not set");
        }

        this.actualConnection = actualConnection;
        this.encryptor = encryptor;
        IncomingAudio = new CryptoPacketChannelReader(this.actualConnection.IncomingAudio, encryptor);
    }

    public async Task SendPacketAsync(Packet packet, CancellationToken cancellationToken)
    {
        var encryptedPacket = await encryptor.EncryptAsync(packet);
        await actualConnection.SendPacketAsync(encryptedPacket, cancellationToken);
    }

    private class CryptoPacketChannelReader : ChannelReader<Packet>
    {
        private readonly ChannelReader<Packet> actualChannelReader;
        private readonly PacketEncryptor encryptor;

        public CryptoPacketChannelReader(ChannelReader<Packet> actualChannelReader, PacketEncryptor encryptor)
        {
            this.actualChannelReader = actualChannelReader;
            this.encryptor = encryptor;
        }

        public override bool TryRead(out Packet item)
        {
            if (!actualChannelReader.TryRead(out item))
                return false;

            item = encryptor.DecryptAsync(item).GetAwaiter().GetResult();  // Todo make the encrypt and decrypt methods sync
            return true;
        }

        public override ValueTask<bool> WaitToReadAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            return actualChannelReader.WaitToReadAsync(cancellationToken);
        }
    }
}

internal class PacketEncryptor : IDisposable
{
    private readonly byte[] aesIV;
    private readonly ECDiffieHellman ecDiffieHellman;
    private byte[]? sharedKey;

    public PacketEncryptor(byte[] aesIV)
    {
        this.aesIV = aesIV;
        ecDiffieHellman = ECDiffieHellman.Create();
    }

    public byte[] GetPublicKey()
    {
        return ecDiffieHellman.PublicKey.ExportSubjectPublicKeyInfo();
    }

    public void SetSharedKey(byte[] otherPartyPublicKey)
    {
        using var otherPartyECDH = ECDiffieHellman.Create();
        otherPartyECDH.ImportSubjectPublicKeyInfo(otherPartyPublicKey, out _);
        sharedKey = ecDiffieHellman.DeriveKeyMaterial(otherPartyECDH.PublicKey);
    }

    public bool IsSharedKeySet => sharedKey != null;

    public async Task<Packet> EncryptAsync(Packet packet)
    {
        if (sharedKey == null)
            throw new InvalidOperationException("Shared key is not set");

        var iv = CalculateIV(packet.SerialNumber);

        using var aes = Aes.Create();
        aes.Key = sharedKey;
        aes.IV = iv;

        var encryptor = aes.CreateEncryptor();

        var encryptedContent = await CryptoTransformAsync(packet.Content, encryptor);
        return packet with { Content = encryptedContent.ToArray() };
    }

    public async Task<Packet> DecryptAsync(Packet encryptedPacket)
    {
        if (sharedKey == null)
            throw new InvalidOperationException("Shared key is not set");

        var iv = CalculateIV(encryptedPacket.SerialNumber);

        using var aes = Aes.Create();
        aes.Key = sharedKey;
        aes.IV = iv;

        var decryptor = aes.CreateDecryptor();

        var decryptedContent = await CryptoTransformAsync(encryptedPacket.Content, decryptor);
        return encryptedPacket with { Content = decryptedContent.ToArray() };
    }

    private static async Task<MemoryStream> CryptoTransformAsync(ReadOnlyMemory<byte> data,
        ICryptoTransform cryptoTransformer)
    {
        var dataStream = new MemoryStream();
        await using var cs = new CryptoStream(dataStream, cryptoTransformer, CryptoStreamMode.Write);
        await cs.WriteAsync(data);
        await cs.FlushFinalBlockAsync();
        return dataStream;
    }

    private byte[] CalculateIV(int serialNumber)
    {
        var serialNumberAsBytes = BitConverter.GetBytes(serialNumber);
        return aesIV
            .Zip(serialNumberAsBytes, (x, y) => (byte)(x ^ y))
            .Concat(aesIV[sizeof(int)..])
            .ToArray();
    }

    public void Dispose()
    {
        ecDiffieHellman.Dispose();
    }
}