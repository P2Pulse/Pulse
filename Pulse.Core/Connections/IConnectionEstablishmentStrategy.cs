using System.Net;

namespace Pulse.Core.Connections;

public interface IConnectionEstablishmentStrategy
{
    Task<IConnection> EstablishConnectionAsync(IPAddress destination, CancellationToken cancellationToken = default);
}