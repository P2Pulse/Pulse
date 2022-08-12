using System.Net;

namespace Pulse.Core.Connections;

public class StreamEstablisher
{
    private readonly IConnectionEstablishmentStrategy _connectionCreator;

    public StreamEstablisher(IConnectionEstablishmentStrategy connectionCreator)
    {
        _connectionCreator = connectionCreator;
    }
    
    public async Task<IBiDirectionalStream> EstablishStreamAsync(IPAddress destination, 
        CancellationToken cancellationToken = default)
    {
        var connection = await _connectionCreator.EstablishConnectionAsync(destination, cancellationToken);
        
        return new UdpBiDirectionalStream(connection);
    }
}