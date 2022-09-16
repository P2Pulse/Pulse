using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using Pulse.Core.Connections;

namespace Pulse.Core;

internal class PacketEncryptor : IDisposable
{
    private readonly ECDiffieHellman ecDiffieHellman;
    private byte[]? aesIV;
    private byte[]? sharedKey;

    public PacketEncryptor()
    {
        ecDiffieHellman = ECDiffieHellman.Create();
    }

    public byte[] PublicKey => ecDiffieHellman.PublicKey.ExportSubjectPublicKeyInfo();

    [MemberNotNullWhen(true, nameof(sharedKey))]
    [MemberNotNullWhen(true, nameof(aesIV))]
    public bool Ready => sharedKey is not null && aesIV is not null;

    public void Dispose()
    {
        ecDiffieHellman.Dispose();
    }

    public void SetOtherPartyPublicKey(byte[] otherPartyPublicKey)
    {
        using var otherPartyECDH = ECDiffieHellman.Create();
        otherPartyECDH.ImportSubjectPublicKeyInfo(otherPartyPublicKey, out _);
        sharedKey = ecDiffieHellman.DeriveKeyMaterial(otherPartyECDH.PublicKey);
    }

    public void SetAesIV(byte[] iv)
    {
        aesIV = iv;
    }

    public async Task<Packet> EncryptAsync(Packet packet)
    {
        if (!Ready)
            throw new InvalidOperationException("Packet encryptor is not sufficiently configured.");

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
        if (!Ready)
            throw new InvalidOperationException("Packet encryptor is not sufficiently configured.");

        var iv = CalculateIV(encryptedPacket.SerialNumber);

        using var aes = Aes.Create();
        aes.Key = sharedKey;
        aes.IV = iv;

        var decryptor = aes.CreateDecryptor();

        var decryptedContent = await CryptoTransformAsync(encryptedPacket.Content, decryptor);
        return encryptedPacket with { Content = decryptedContent.ToArray() };
    }

    private static async Task<MemoryStream> CryptoTransformAsync(
        ReadOnlyMemory<byte> data,
        ICryptoTransform cryptoTransformer
    )
    {
        var dataStream = new MemoryStream();
        await using var cs = new CryptoStream(
            dataStream,
            cryptoTransformer,
            CryptoStreamMode.Write
        );
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
}