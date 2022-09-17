using System.Threading.Channels;

namespace Pulse.Core.Connections;

internal interface IConnection : IAsyncDisposable
{
    ChannelReader<Packet> IncomingAudio { get; }
    Task SendPacketAsync(Packet packet, CancellationToken cancellationToken);
}