using System.Net.Http.Json;

namespace Pulse.Core.Calls;

public class IncomingCallPoller
{
    private readonly HttpClient httpClient;
    private readonly ICallAcceptor callAcceptor;
    private const string Endpoint = "/calls";

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
            try
            {
                var callRequest = await httpClient.GetFromJsonAsync<CallRequest>(Endpoint, cancellationToken: ct);
                
                if (callRequest.calling)
                {
                    var callerUsername = callRequest.username;
                    Console.WriteLine("Call from: " + callerUsername);
                    // TODO: interact with the user
                    var audioStream = await callAcceptor.AnswerCallAsync(ct);
                    // write stream to file
                    await using var fileStream = File.Create("output.wav");
                    await audioStream.CopyToAsync(fileStream, ct);
                    Console.WriteLine("Call hanged");
                }
                else
                {
                    await Task.Delay(250, ct);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}