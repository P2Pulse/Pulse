using System.Net;
using Pulse.Core.Connections;

var strategy = new StreamEstablisher(new PortBruteForceNatTraversal());
Console.WriteLine("Enter the other person's IP address: ");
var destination = IPAddress.Parse(Console.ReadLine()!);

var stream = await strategy.EstablishStreamAsync(destination);

await using var output = File.OpenWrite("output.wav");
await stream.Input.CopyToAsync(output);

/*await Task.Delay(200);
await using var input = File.OpenRead("input.wav");
await input.CopyToAsync(stream.Output);*/

await Task.Delay(-1);