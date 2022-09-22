using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using Pulse.Server.Contracts;

namespace Pulse.Core.Calls;

public class IncomingCallPoller
{
    private readonly HttpClient httpClient;
    private const string Endpoint = "/calls/incoming";

    public IncomingCallPoller(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    public async Task<string?> PollAsync(CancellationToken ct = default)
    {
        var pollingResponse = await httpClient.GetAsync(Endpoint, cancellationToken: ct);

        if (pollingResponse.StatusCode is HttpStatusCode.NotFound)
            return null;

        pollingResponse.EnsureSuccessStatusCode();

        var incomingCall = await pollingResponse.Content.ReadFromJsonAsync<IncomingCall>(cancellationToken: ct);
        var callerUsername = incomingCall!.Username;
        Console.WriteLine("Call from: " + callerUsername);
        return callerUsername;
    }
}