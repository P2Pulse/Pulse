namespace Pulse.Core.Connections;

internal static class PacketEncoder
{
    private const int SerialNumberSize = sizeof(int);
    
    public static Packet Decode(ReadOnlyMemory<byte> rawMessage)
    {
        var serialNumber = BitConverter.ToInt32(rawMessage[..SerialNumberSize].Span);
        return new Packet(serialNumber, rawMessage[SerialNumberSize..]);
    }

    public static ReadOnlyMemory<byte> Encode(Packet packet)
    {
        var encodedMessage = new byte[packet.Content.Length + SerialNumberSize].AsMemory();
        BitConverter.GetBytes(packet.SerialNumber).CopyTo(encodedMessage);
        packet.Content.CopyTo(encodedMessage[SerialNumberSize..]);
        return encodedMessage;
    }
}