using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using Pulse.Core;
using Pulse.Core.Calls;

Console.Write("Initiator or Receiver? i/r");

var answer = Console.ReadLine();

using var httpClient = new HttpClient
{
    BaseAddress = new Uri("http://ec2-3-65-21-97.eu-central-1.compute.amazonaws.com:5000"),
    DefaultRequestHeaders =
    {
        Authorization = new AuthenticationHeaderValue("Bearer",
            "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJndXIiLCJuYmYiOjE2NjI3Mzc0NzcsImV4cCI6MTY2MzM0MjI3NywiaWF0IjoxNjYyNzM3NDc3fQ.uqXbNJsGZDyb69qNEInK7KBs1muBcvJENll6O_Pdrjs")
    }
};

var services = new ServiceCollection();
const string serverHttpClient = "Pulse.Server";
services.AddHttpClient(serverHttpClient, client =>
{
    client.BaseAddress = new Uri("http://localhost:5006");
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "{MY_TOKEN}");
});
services.AddTransient(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient(serverHttpClient));
services.AddPulse();

var serviceProvider = services.BuildServiceProvider();

if (answer == "i")
{
    Console.Write("Who do you want to call? ");
    var callee = Console.ReadLine();
    Console.WriteLine("Calling...");
    var callInitiator = serviceProvider.GetRequiredService<ICallInitiator>();
    var stream = await callInitiator.CallAsync(callee!);

    await using var file = File.OpenRead("music.wav");
    await file.CopyToAsync(stream);
}
else
{
    Console.WriteLine("Polling...");
    _ = serviceProvider.GetRequiredService<IncomingCallPoller>();
}


Console.WriteLine("Done");

await Task.Delay(-1);