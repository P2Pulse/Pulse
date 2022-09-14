using System.Net;
using System.Net.Http.Json;
using Pulse.Core.AudioStreaming;
using Pulse.Core.Connections;

namespace Pulse.Core.Calls;

public class CallAcceptor
{
    private const string Endpoint = "/calls/accept";
    private readonly HttpClient httpClient;
    private readonly PortBruteForceNatTraversal portBruteForcer;

    public CallAcceptor(HttpClient httpClient)
    {
        this.httpClient = httpClient;
        portBruteForcer = new PortBruteForceNatTraversal();
    }

    public async Task<Stream> AnswerCallAsync(CancellationToken ct = default)
    {
        // TODO: extract common logic between here and CallInitiator, to a shared function
        var (myIPv4Address, min, max) = await portBruteForcer.PredictMinMaxPortsAsync(ct);
        var body = new
        {
            calleeIPv4Address = myIPv4Address.ToString(),
            minPort = min,
            maxPort = max,
        };
        Console.WriteLine("Answering call...");
        var response = await httpClient.PostAsJsonAsync(Endpoint, body, cancellationToken: ct);
        response.EnsureSuccessStatusCode();

        var (remoteIpAddress, minPort, maxPort) =
            (await response.Content.ReadFromJsonAsync<ConnectionInfo>(cancellationToken: ct))!;

        var connection = await portBruteForcer.EstablishConnectionAsync(IPAddress.Parse(remoteIpAddress), minPort,
            maxPort, ct);

        return new PacketStream(connection);
    }
}