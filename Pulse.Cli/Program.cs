using System.Net;
using Pulse.Core.Connections;

var strategy = new PortBruteForceNatTraversal();
var destination = IPAddress.Parse("12.34.56.68");
await strategy.EstablishConnectionAsync(destination);
await Task.Delay(-1);