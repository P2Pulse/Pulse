using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using Pulse.Core;
using Pulse.Core.Calls;

var services = new ServiceCollection();
const string serverHttpClient = "Pulse.Server";
services.AddHttpClient(serverHttpClient, client =>
{
    client.BaseAddress = new Uri("https://pulse.gurgaller.com");
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
        "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJ5b3RhbSIsIm5iZiI6MTY2MzkzNTY3OSwiZXhwIjoxNjY0NTQwNDc5LCJpYXQiOjE2NjM5MzU2Nzl9.eIrF51xJl0hh817vTJOjY7Olrpp8mwTMhUsDLj-lhDM");
});
services.AddTransient(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient(serverHttpClient));
services.AddPulse();

var serviceProvider = services.BuildServiceProvider();

Console.Write("Initiator or Receiver? i/r");

var answer = Console.ReadLine();

if (answer == "i")
{
    Console.Write("Who do you want to call? ");
    var callee = Console.ReadLine();
    Console.WriteLine("Calling...");
    var callInitiator = serviceProvider.GetRequiredService<ICallInitiator>();
    var audioStream = await callInitiator.CallAsync(callee!);
    
    await using var fileStream = File.Create("output.wav");
    await using var file = File.OpenRead("music.wav");
    
    var receive = audioStream.CopyToAsync(fileStream);
    var send = file.CopyToAsync(audioStream, bufferSize: 320);
    await Task.WhenAll(send, receive);
}
else
{
    File.Delete("output.wav");
    Console.WriteLine("Polling...");
    var poller = serviceProvider.GetRequiredService<IncomingCallPoller>();
    string? username = null;
    while ((username = await poller.PollAsync()) is null)
    {
        await Task.Delay(200);
    }
    Console.WriteLine($"Incoming call from {username}");
    var callAcceptor = serviceProvider.GetRequiredService<ICallAcceptor>();
    await using var stream = await callAcceptor.AnswerCallAsync();
    await using var fileStream = File.Create("output.wav");
    await stream.CopyToAsync(fileStream);
}


Console.WriteLine("Done");

await Task.Delay(-1);