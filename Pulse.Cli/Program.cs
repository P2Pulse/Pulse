using System.Diagnostics;
using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using Pulse.Core;
using Pulse.Core.Calls;

var services = new ServiceCollection();
const string serverHttpClient = "Pulse.Server";
services.AddHttpClient(serverHttpClient, client =>
{
    client.BaseAddress = new Uri("http://ec2-3-65-21-97.eu-central-1.compute.amazonaws.com:5000");
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
        "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJ5b3RhbSIsIm5iZiI6MTY2MzQxNDg5NywiZXhwIjoxNjY0MDE5Njk3LCJpYXQiOjE2NjM0MTQ4OTd9.QEbafJDu3GVVaefZdtZEKIWCaS0-OLgsaGTz05tNSAE");
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
    var stream = await callInitiator.CallAsync(callee!);

    await using var file = File.OpenRead("music.wav");
    
    var sw = Stopwatch.StartNew();
    await file.CopyToAsync(stream, bufferSize: 320);
    sw.Stop();
    Console.WriteLine($"Sent {file.Length} bytes in {sw.Elapsed.TotalSeconds} seconds");
}
else
{
    Console.WriteLine("Polling...");
    _ = serviceProvider.GetRequiredService<IncomingCallPoller>();
}


Console.WriteLine("Done");

await Task.Delay(-1);