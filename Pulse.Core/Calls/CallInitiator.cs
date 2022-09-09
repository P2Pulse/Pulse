using System.Net;
using System.Net.Http.Json;
using Pulse.Core.AudioStreaming;
using Pulse.Core.Connections;

namespace Pulse.Core.Calls;

public class CallInitiator
{
    private const string Endpoint = "/calls";
    private readonly HttpClient httpClient;
    private readonly PortBruteForceNatTraversal portBruteForcer;

    public CallInitiator(HttpClient httpClient)
    {
        this.httpClient = httpClient;
        portBruteForcer = new PortBruteForceNatTraversal();
    }

    public async Task<Stream> CallAsync(string username, CancellationToken ct = default)
    {
        var (min, max) = await portBruteForcer.PredictMinMaxPortsAsync(ct);
        var body = new
        {
            calleeUserName = username,
            minPort = min,
            maxPort = max,
        };
        var response = await httpClient.PostAsJsonAsync(Endpoint, body, cancellationToken: ct);
        response.EnsureSuccessStatusCode();

        var (remoteIpAddress, minPort, maxPort) =
            (await response.Content.ReadFromJsonAsync<ConnectionInfo>(cancellationToken: ct))!;

        var connection = await portBruteForcer.EstablishConnectionAsync(IPAddress.Parse(remoteIpAddress), minPort,
            maxPort, ct);

        return new PacketStream(connection);
    }
}