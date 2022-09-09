using Pulse.Core.Calls;

Console.Write("Initiator or Receiver? i/r");

var answer = Console.ReadLine();

var httpClient = new HttpClient
{
    BaseAddress = new Uri("http://SOMETHINGSOMETHING.eu-central-1.compute.amazonaws.com")
};

if (answer == "i")
{
    Console.WriteLine("Calling...");
    var callInitiator = new CallInitiator(httpClient);
    var stream = await callInitiator.CallAsync("gur");

    await using var file = File.OpenRead("music.wav");
    await file.CopyToAsync(stream);
}
else
{
    Console.WriteLine("Polling...");
    var callPoller = new IncomingCallPoller(httpClient);
}


Console.WriteLine("Done");

await Task.Delay(-1);