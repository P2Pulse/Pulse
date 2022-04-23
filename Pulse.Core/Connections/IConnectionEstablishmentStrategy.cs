using System.Net;

namespace Pulse.Core.Connections;

internal interface IConnectionEstablishmentStrategy
{
    Task<IConnection> EstablishConnectionAsync(IPAddress destination, CancellationToken cancellationToken = default);
}