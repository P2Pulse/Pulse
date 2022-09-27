using System.Net.Http.Json;
using Pulse.Server.Contracts;

namespace Pulse.Core.Calls;

internal class UdpCallAcceptor : ICallAcceptor
{
    private const string Endpoint = "/calls/join";
    private readonly UdpStreamFactory connectionFactory;
    private readonly HttpClient httpClient;

    public UdpCallAcceptor(HttpClient httpClient, UdpStreamFactory connectionFactory)
    {
        this.httpClient = httpClient;
        this.connectionFactory = connectionFactory;
    }

    public async Task<Stream> AnswerCallAsync(CancellationToken ct = default)
    {
        return await connectionFactory.ConnectAsync(async myInfo =>
        {
            Console.WriteLine("Answering call...");
            var response = await httpClient.PostAsJsonAsync(Endpoint, myInfo, ct);
            try
            {
                response.EnsureSuccessStatusCode();
            }
            catch (Exception e)
            {
                throw new Exception(await response.Content.ReadAsStringAsync(ct), e);
            }

            return (await response.Content.ReadFromJsonAsync<ConnectionDetails>(cancellationToken: ct))!;
        }, isInitiator: false , ct);
    }
}