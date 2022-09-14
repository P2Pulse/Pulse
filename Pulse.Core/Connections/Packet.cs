namespace Pulse.Core.Connections;

internal record Packet(int SerialNumber, ReadOnlyMemory<byte> Content);