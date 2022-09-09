using System.Net;
using System.Net.Http.Json;
using Pulse.Core.AudioStreaming;
using Pulse.Core.Connections;

namespace Pulse.Core.Calls;

public class IncomingCallPoller
{
    private readonly HttpClient httpClient;
    private const string Endpoint = "/calls";

    public IncomingCallPoller(HttpClient httpClient)
    {
        this.httpClient = httpClient;
        _ = PollAsync();
    }

    private async Task PollAsync(CancellationToken ct = default)
    {
        while (!ct.IsCancellationRequested)
        {
            var hasCall = await httpClient.GetFromJsonAsync<bool>(Endpoint, cancellationToken: ct);
            
            if (hasCall)
            {
                // TODO: interact with the user
                var callAcceptor = new CallAcceptor(httpClient);
                var audioStream = await callAcceptor.AnswerCallAsync(ct);
                // write stream to file
                await using var fileStream = File.Create("output.wav");
                await audioStream.CopyToAsync(fileStream, ct);
                Console.WriteLine("Done file");
            }
            else
            {
                await Task.Delay(250, ct);
            }
        }
        
    }
}