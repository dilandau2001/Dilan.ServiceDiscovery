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

client.Options.ServiceName = serviceName;
client.Options.CallbackPort = int.Parse(portText ?? "5000");
client.Options.Scope = "End1";
client.ExtraData["test"] = "hello";


// this will raise 
await client.Start();

Console.WriteLine("1 for as for service");
Console.WriteLine("Press other to finish");
string? s = Console.ReadLine();

while (s=="1")
{
    var respone = await client.FindService(client.Options.ServiceName, client.Options.Scope);

    foreach (var r in respone.Services)
    {
        Console.WriteLine($"Found = {r.ServiceName}, {r.ServiceHost}, {r.ServicePort}, {r.Scope}");
    }

    Console.WriteLine("1 for as for service");
    s = Console.ReadLine();
}

Console.WriteLine("End");
