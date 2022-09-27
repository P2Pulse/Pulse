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
        try
        {
            initiationResponse.EnsureSuccessStatusCode();
        }
        catch (Exception e)
        {
            throw new Exception(await initiationResponse.Content.ReadAsStringAsync(ct), e);
        }

        return await connectionFactory.ConnectAsync(async myInfo =>
        {
            var response = await httpClient.PostAsJsonAsync(Endpoint + "/join", myInfo, ct);
            try
            {
                response.EnsureSuccessStatusCode();
            }
            catch (Exception e)
            {
                throw new Exception(await response.Content.ReadAsStringAsync(ct), e);
            }

            Console.WriteLine("The other person answered the call");

            return (await response.Content.ReadFromJsonAsync<ConnectionDetails>(cancellationToken: ct))!;
        }, isInitiator: true, ct);
    }
}