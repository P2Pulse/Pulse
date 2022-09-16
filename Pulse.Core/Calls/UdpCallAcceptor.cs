using System.Net.Http.Json;

namespace Pulse.Core.Calls;

internal class UdpCallAcceptor : ICallAcceptor
{
    private const string Endpoint = "/calls/accept";
    private readonly UdpStreamFactory connectionFactory;
    private readonly HttpClient httpClient;

    public UdpCallAcceptor(HttpClient httpClient, UdpStreamFactory connectionFactory)
    {
        this.httpClient = httpClient;
        this.connectionFactory = connectionFactory;
    }

    public async Task<Stream> AnswerCallAsync(CancellationToken ct = default)
    {
        return await connectionFactory.ConnectAsync(
            async myInfo =>
            {
                var body = new
                {
                    calleeIPv4Address = myInfo.IPAddress,
                    minPort = myInfo.MinPort,
                    maxPort = myInfo.MaxPort,
                    publicKey = myInfo.PublicKey
                };
                Console.WriteLine("Answering call...");
                var response = await httpClient.PostAsJsonAsync(
                    Endpoint,
                    body,
                    ct
                );
                response.EnsureSuccessStatusCode();

                return (await response.Content.ReadFromJsonAsync<ConnectionInfo>(cancellationToken: ct))!;
            },
            ct
        );
    }
}