using System.Net;
using Pulse.Core.Connections;

namespace Pulse.Core.AudioStreaming;

internal class StreamEstablisher
{
    private readonly IConnectionEstablishmentStrategy connectionCreator;

    public StreamEstablisher(IConnectionEstablishmentStrategy connectionCreator)
    {
        this.connectionCreator = connectionCreator;
    }
    
    public async Task<Stream> EstablishStreamAsync(IPAddress destination, 
        CancellationToken cancellationToken = default)
    {
        var connection = await connectionCreator.EstablishConnectionAsync(destination, cancellationToken);
        
        return new PacketStream(connection);
    }
}