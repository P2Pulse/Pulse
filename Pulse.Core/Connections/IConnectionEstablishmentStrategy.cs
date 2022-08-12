using System.Net;
using System.Net.Sockets;

namespace Pulse.Core.Connections;

public interface IConnectionEstablishmentStrategy
{
    Task<Socket> EstablishConnectionAsync(IPAddress destination, CancellationToken cancellationToken = default);
}