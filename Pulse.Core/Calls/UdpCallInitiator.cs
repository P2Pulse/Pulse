using System.Net.Http.Json;

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
        return await connectionFactory.ConnectAsync(
            async myInfo =>
            {
                var body = new
                {
                    IPv4Address = myInfo.IPAddress,
                    calleeUserName = username,
                    minPort = myInfo.MinPort,
                    maxPort = myInfo.MaxPort,
                    publicKey = myInfo.PublicKey
                };
                var response = await httpClient.PostAsJsonAsync(
                    Endpoint,
                    body,
                    ct
                );
                response.EnsureSuccessStatusCode();

                Console.WriteLine("The other person answered the call");

                return (await response.Content.ReadFromJsonAsync<ConnectionInfo>(cancellationToken: ct))!;
            },
            ct
        );
    }
}