using System.Net;
using System.Net.Http.Json;
using Pulse.Server.Contracts;

namespace Pulse.Core.Calls;

public class IncomingCallPoller
{
    private readonly HttpClient httpClient;
    private readonly ICallAcceptor callAcceptor;
    private const string Endpoint = "/calls/incoming";

    public IncomingCallPoller(HttpClient httpClient, ICallAcceptor callAcceptor)
    {
        this.httpClient = httpClient;
        this.callAcceptor = callAcceptor;
        _ = PollAsync();
    }

    private async Task PollAsync(CancellationToken ct = default)
    {
        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(250, ct);
            try
            {
                var pollingResponse = await httpClient.GetAsync(Endpoint, cancellationToken: ct);
                if (pollingResponse.StatusCode is HttpStatusCode.NotFound)
                    continue;

                pollingResponse.EnsureSuccessStatusCode();

                var incomingCall = await pollingResponse.Content.ReadFromJsonAsync<IncomingCall>(cancellationToken: ct);
                var callerUsername = incomingCall!.Username;
                Console.WriteLine("Call from: " + callerUsername);
                // TODO: interact with the user
                var audioStream = await callAcceptor.AnswerCallAsync(ct);
                // write stream to file
                await using var fileStream = File.Create("output.wav");
                await audioStream.CopyToAsync(fileStream, ct);
                Console.WriteLine("Call hanged");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}