using System.Threading.Channels;
using Pulse.Core.AudioStreaming;

namespace Pulse.Core.Connections;

internal interface IConnection
{
    ChannelReader<Packet> IncomingAudio { get; }
    Task SendPacketAsync(Packet packet, CancellationToken cancellationToken);
}