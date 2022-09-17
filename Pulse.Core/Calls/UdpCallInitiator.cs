using System.Net.Http.Json;
using Pulse.Server.Contracts;

namespace Pulse.Core.Calls;

internal class UdpCallInitiator : ICallInitiator
{
    private const string Endpoint = "/calls";
    private readonly UdpStreamFactory connectionFactory;
    private readonly HttpClient httpClient;

    public UdpCallInitiator(HttpClient httpClient, UdpStreamFactory connectionFactory)
    {
        this.httpClient = httpClient;
        this.connectionFactory = connectionFactory;
    }

    public async Task<Stream> CallAsync(string username, CancellationToken ct = default)
    {
        var initiationResponse = await httpClient.PostAsJsonAsync(Endpoint, new InitiateCallRequest(username), ct);
        initiationResponse.EnsureSuccessStatusCode();
        
        return await connectionFactory.ConnectAsync(async myInfo =>
        {
            var response = await httpClient.PostAsJsonAsync(Endpoint + "/join", myInfo, ct);
            response.EnsureSuccessStatusCode();

            Console.WriteLine("The other person answered the call");

            return (await response.Content.ReadFromJsonAsync<ConnectionDetails>(cancellationToken: ct))!;
        }, ct);
    }
}