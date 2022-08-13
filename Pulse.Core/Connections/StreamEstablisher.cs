using System.Net;

namespace Pulse.Core.Connections;

public class StreamEstablisher
{
    private readonly IConnectionEstablishmentStrategy connectionCreator;

    public StreamEstablisher(IConnectionEstablishmentStrategy connectionCreator)
    {
        this.connectionCreator = connectionCreator;
    }
    
    public async Task<IBiDirectionalStream> EstablishStreamAsync(IPAddress destination, 
        CancellationToken cancellationToken = default)
    {
        var connection = await connectionCreator.EstablishConnectionAsync(destination, cancellationToken);
        
        return new UdpBiDirectionalStream(connection);
    }
}