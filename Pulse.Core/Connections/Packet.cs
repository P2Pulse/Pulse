namespace Pulse.Core.Connections;

internal record Packet(ReadOnlyMemory<byte> Content);