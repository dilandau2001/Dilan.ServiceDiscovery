// See https://aka.ms/new-console-template for more information

using Dilan.GrpcServiceDiscovery.Grpc;
// ReSharper disable AccessToDisposedClosure

int port = 5478;
string multicastGroup = "224.0.0.100";
string miip = StaticHelpers.GetLocalIpAddress();

// By default I am a client
MulticastClient client = new MulticastClient(new ConsoleLogger<MulticastClient>());

Console.WriteLine("1 - Client");
Console.WriteLine("2 - Server");
var i = Console.ReadLine();

if (i == "1")
{
    var tempo = new System.Timers.Timer(1000);
    tempo.Elapsed += (sender, eventArgs) => client.Send($" ip:{miip}, port:{port}", multicastGroup, port);
    tempo.Start();
}
else
{
    client.DataReceived += (sender, data) => Console.WriteLine($"{DateTime.Now} - Received: {data.Message} from {data.Source}");
    client.StartService(port, multicastGroup);
}

Console.ReadLine();
Console.WriteLine("Closing...");
client.Dispose();