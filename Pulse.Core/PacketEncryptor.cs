using System.Security.Cryptography;
using Pulse.Core.Connections;

namespace Pulse.Core;

public static class Tester
{
    public static async Task Main()
    {
        var aesIV = Aes.Create().IV;

        using var alice = new PacketEncryptor(aesIV);
        using var bob = new PacketEncryptor(aesIV);

        alice.SetSharedKey(bob.GetPublicKey());
        bob.SetSharedKey(alice.GetPublicKey());
        var packet = new Packet(3, BitConverter.GetBytes(1234567890));
        var encrypted = await alice.EncryptAsync(packet);
        var decrypted = await bob.DecryptAsync(encrypted);
        Console.WriteLine("Result:");
        Console.WriteLine(BitConverter.ToInt32(decrypted.Content.Span));
    }
}

public class PacketEncryptor : IDisposable
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

    internal async Task<Packet> EncryptAsync(Packet packet)
    {
        if (sharedKey == null)
            throw new InvalidOperationException("Shared key is not set");

        var iv = CalculateIV(packet.SerialNumber);

        using var aes = Aes.Create();
        aes.Key = sharedKey;
        aes.IV = iv;

        using var ciphertext = new MemoryStream();

        await using var cs = new CryptoStream(ciphertext, aes.CreateEncryptor(), CryptoStreamMode.Write);
        await cs.WriteAsync(packet.Content);
        await cs.FlushFinalBlockAsync();

        return packet with { Content = ciphertext.ToArray() };
    }

    internal async Task<Packet> DecryptAsync(Packet encryptedPacket)
    {
        if (sharedKey == null)
            throw new InvalidOperationException("Shared key is not set");

        var iv = CalculateIV(encryptedPacket.SerialNumber);

        using var aes = Aes.Create();
        aes.Key = sharedKey;
        aes.IV = iv;

        using var decryptedMessage = new MemoryStream(encryptedPacket.Content.ToArray());
        await using var cs = new CryptoStream(decryptedMessage, aes.CreateDecryptor(), CryptoStreamMode.Read);
        var contentStream = new MemoryStream();
        await cs.CopyToAsync(contentStream);
        await cs.FlushFinalBlockAsync();
        return encryptedPacket with { Content = contentStream.ToArray() };
    }

    private byte[] CalculateIV(int serialNumber)
    {
        var serialNumberAsBytes = BitConverter.GetBytes(serialNumber);
        return aesIV
            .Zip(serialNumberAsBytes, (x, y) => x ^ y)
            .Cast<byte>()
            .Concat(aesIV[sizeof(int)..])
            .ToArray();
    }

    public void Dispose()
    {
        ecDiffieHellman.Dispose();
    }
}