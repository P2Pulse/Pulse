using Microsoft.Extensions.DependencyInjection;
using Pulse.Cli;
using Pulse.Core;
using Pulse.Core.Authentication;
using Pulse.Core.Calls;

var services = new ServiceCollection();
const string myToken = "MY_TOKEN";
services.AddSingleton<IAccessTokenStorage>(new ImmutableAccessTokenStorage(myToken));
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