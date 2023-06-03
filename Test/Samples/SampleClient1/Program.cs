using System.Diagnostics;
using Dilan.GrpcServiceDiscovery.Grpc;

Console.WriteLine("Hello, World!");

var client = new ServiceDiscoveryClient(
    new ConsoleLogger<ServiceDiscoveryClient>(),
    new ClientConfigurationOptions(),
    new MulticastClient(new ConsoleLogger<MulticastClient>()),
    new List<IMetadataProvider>()
    {
        new SystemInfoMetadataProvider()
    });

Console.WriteLine("Enter Service Name");
var serviceName = Console.ReadLine();

Console.WriteLine("Enter Port Number");
var portText = Console.ReadLine();

client.Options.DiscoveryServerHost = "localhost";
client.Options.ServiceName = serviceName;
client.Options.CallbackPort = int.Parse(portText ?? "5000");
client.Options.Scope = "End1";
client.ExtraData["test"] = "hello";


// this will raise 
await client.Start();

Console.WriteLine("1 for ask for service");
Console.WriteLine("Press other to finish");
string? s = Console.ReadLine();

while (s=="1")
{
    Stopwatch w = new Stopwatch();
    w.Start();
    var response = await client.FindService(client.Options.ServiceName, string.Empty);
    w.Stop();

    foreach (var r in response.Services)
    {
        Console.WriteLine($"Found = {r.ServiceName}, {r.ServiceHost}, {r.ServicePort}, {r.Scope}");
    }

    Console.WriteLine("Request took: " + w.Elapsed.TotalMilliseconds + " ms.");

    Console.WriteLine("1 for ask for service");
    s = Console.ReadLine();
}

Console.WriteLine("End");
