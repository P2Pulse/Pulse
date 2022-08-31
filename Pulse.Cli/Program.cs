using System.Net;
using Pulse.Core.Connections;

var strategy = new StreamEstablisher(new PortBruteForceNatTraversal());
Console.WriteLine("Enter the other person's IP address: ");
var destination = IPAddress.Parse(Console.ReadLine()!);

await using var stream = await strategy.EstablishStreamAsync(destination);
Console.WriteLine("Yotam asked me to print this");
await using var output = File.OpenWrite("output.wav");
await stream.Input.CopyToAsync(output, bufferSize: 512);

/*await Task.Delay(200);
await using var input = File.OpenRead("input.wav");
await input.CopyToAsync(stream.Output);*/

await Task.Delay(-1);