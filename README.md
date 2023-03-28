# Dilan.ServiceDiscovery

[![standard-readme compliant](https://img.shields.io/badge/readme%20style-standard-brightgreen.svg?style=flat-square)](https://github.com/RichardLitt/standard-readme)

[![NuGet version (Dilan.GrpcServiceDiscovery.Grpc)](https://img.shields.io/nuget/v/Dilan.GrpcServiceDiscovery.Grpc?style=flat-square)](https://www.nuget.org/packages/Dilan.GrpcServiceDiscovery.Grpc/)

![Code Coverage](https://img.shields.io/badge/Code%20Coverage-70%25-yellow?style=flat)

This library provides a fast, easy to setup way to implement a discovery service pattern within any .NET solution/project.
Although there are several other more complex solutions out there that you might have consider, this one of mine have some features you might find matching your problem
as it matched mine.
- This library is fully developed in C# for both server and client sides.
- The library itself is netstandard 2.0, which means it is compatible with both full framework and Core frameworks up to now.
- This library comes with both server and client side implemented in a single dll.
- This library does not make use of Core Web app libraries to accompish its purpose. Most of discovery services out there are based on Web apps, which in the C# core world it means to have a strong dependency on Core Web app libraries and their dependency hell. You won't be able to have Full framework services and use Web Apps with updated libraries.
- This library is coded having dependency injection in mind. Most behaviours can be configured and changed by modifying registered items in whatever injection framework you decide to use.
- This library comes with an auto-discover logic that reduces the number of parameters to be configured.
- This library supports SSL security.


Dependencies:
- For every logic I strong believe in the use of state machines. I really like the use of Appcelerator as my state machine framework and that is one of the few dependencies this library has from nuget.
- The other one is GRPC. GRPC is very popular nowadays as a fast and reliable communication system which is Open Source, supported by Google and multi-platform.

The BlazorServer that is also available in this library is just a front end of the server logic and thus, its use is optional. If you prefer running the discovery server in other application you can do so very easily.


## Table of Contents

- [Background](#Background)
- [Install](#install)
- [Usage](#usage)
- [API](#api)
- [Contributing](#contributing)
- [Pending](#Pending)
- [License](#license)

## Background

The need for this library came from a project I was working on where we had several web apps that were develop using Core 2.0. Those appps were dependant on Microsoft
core web app packages. In this project we also had some services running as windows service, using framework 4.8 and we were lucky or unlucky to share several common libraries as those services were also making use of Microsoft web packages for health checks.
Due to cybersecurity reasons we were forced to upgrade our web apps to updated versions of Core.Web libraries, which seemed to be impossible without destroying many things, because new libraries were not compatible with 4.8 framework.

By using this discovery service pattern that is based in GRCP we were able to reduce services configuration and the dependency on Core.Web libraries. (which by the way is a real pain)

I know is not the current trend, where everything has to be a wep application to be "modern", but this is a clean solution to have a discovery server pattern that would be used with several different services wihout adding a ton of core.web dependencies
and allowing having functionality between full framework and updated web apps, while still being multi platform.

With this library you will be able to still have old style window full framework services, that can register into the service discovery and be communicated with.
With the same library you can have state of the art web apps that can register into the service discovery and be communicated with.

This discovery server now becames a healthCheck provider, a DNS server and a load balancer, all together in a small, efficient and fast web application.

This library comes in two assemblies:
- Gprc dll assembly is where you will find both client, and server implementations.
- Blazor server app, you can consider it like a sample application that make use of the Gprc server side and work as a front end for it.

## Install

Easiest way is download the code and compile it. You can also get it from nuget or from github releases.

### The Library

nuget package available
https://www.nuget.org/packages/Dilan.GrpcServiceDiscovery.Grpc


### The Blazor Server app

See release assets:
https://github.com/dilandau2001/Dilan.ServiceDiscovery/releases

## Usage

### Server side.

The server constructor looks like this

```

        public ServiceDiscoveryServer(
            ILogger<ServiceDiscoveryServer> logger,
            IServerManagerLogic logic,
            ServiceConfigurationOptions options,
            IMulticastClient client)

```

Where
- logger: Is the logging instance you are using for tracing. This library provides a ConsoleLogger. You can use your own or default microsoft ones.
- logic: Where the list of registered service and the logic lies. You would use a ServerManagerLogic instance.
- options: Where the server option lie.
- client: multicast client that is used for the autodiscovery feature if configured.

If you are not using dependency injection, and you like default options, you would create an instance and start your discovery server like this:

```
        var options = new ServiceConfigurationOptions();

        var server = new ServiceDiscoveryServer(
            new ConsoleLogger<ServiceDiscoveryServer>(),
            new ServerManagerLogic(options),
            options,
            new MulticastClient(new ConsoleLogger<MulticastClient>()));

        server.Start();

```

Server options are:

```

        /// <summary>
        /// Gets or sets the listening port for the server.
        /// <remarks>By default is 6000</remarks>
        /// </summary>
        public int Port { get; set; } = 6000;

        /// <summary>
        /// Gets or sets the refresh time passed to clients.
        /// This is the time the server will communicate the clients they have to refresh its status.
        /// <remarks>By default is 1 second</remarks>
        /// </summary>
        public int RefreshTimeInSeconds { get; set; } = 1;

        /// <summary>
        /// Gets or sets the time out in seconds.
        /// This is the time the server will wait for incoming registrations.
        /// If a client service fails to register in less than this time then the service is configured to offline.
        /// Note this time should be higher than the refresh time so the clients have plenty of time to even miss a few messages.
        /// </summary>
        public int TimeOutInSeconds { get; set; } = 5;

        /// <summary>
        /// Time in milliseconds for the timer that is checking if there are any service time out.
        /// </summary>
        public int TimeOutCheckingTimeMs { get; set; } = 1000;

        /// <summary>
        /// Enables auto discovery.
        /// If enabled the server will be sending a multicast messages to the AutoDiscoverMulticastGroup,
        /// with a frequency in seconds of AutoDiscoverFreq and to the port AutoDiscoverPort.
        /// Clients can capture this message to retrieve discovery service ip and port.
        /// </summary>
        public bool EnableAutoDiscover { get; set; } = true;

        /// <summary>
        /// Auto discovery multicast group.
        /// </summary>
        public string AutoDiscoverMulticastGroup { get; set; } = "224.0.0.100";

        /// <summary>
        /// Auto discovery multicast port.
        /// </summary>
        public int AutoDiscoverPort { get; set; } = 5478;

        /// <summary>
        /// Auto discovery send data frequency.
        /// </summary>
        public int AutoDiscoverFreq { get; set; } = 5;

        /// <summary>
        /// Gets or sets a value indicating whether [use secure connection].
        /// If secure connection is true and certificate name is found in the machine.
        /// </summary>
        /// <value>
        ///   <c>true</c> if [use secure connection]; otherwise, <c>false</c>.
        /// </value>
        public bool UseSecureConnection { get; set; } = true;

        /// <summary>
        /// Gets or sets the name of the certificate issuer name.
        /// </summary>
        /// <value>
        /// The name of the certificate issuer name.
        /// </value>
        public string CertificateIssuerName { get; set; } = "dilan.ServiceDiscovery";

        /// <summary>
        /// Gets or sets a value indicating whether [use certificate file].
        /// If this setting is set to true and UseSecureConnection is true then the Certificate file
        /// is searched inside the application folder.
        /// If this setting is false, then the certificate is searched in the Computer certificate repository.
        /// (In windows the Manage Workstation Certificates)
        /// </summary>
        /// <value>
        ///   <c>true</c> if [user certificate file]; otherwise, <c>false</c>.
        /// </value>
        public bool UseCertificateFile { get; set; } = false;

        /// <summary>
        /// Gets or sets the use certificate file password.
        /// When UseCertificateFile is used, in order to open the certificate file name.pfx you need
        /// to pass the password in order to get the private key.
        /// </summary>
        /// <value>
        /// The use certificate file password.
        /// </value>
        public string UseCertificateFilePassword { get; set; } = "dilandau2001";

```

If you use the BlazorServer server, this is done for you.

### Server side: Blazor Server App

The BlazorServer is a Blazor application that provide a nice front end of the server discovery. 
As it is based in a Blazor application it can be run in several ways:
- You can deploy it in a Docker container.
- You can run it as dot net application by using dot net run.
- You can run it as console (but you have to do it as administrator)
- You can run it within visual studio.
- You can deploy it as a web app inside a IISS server.
- You can run the published executable

Service configuration is read from app settings and can be manually modified to match your needs.

Current settings look like this:

```
{
  "Logging": {
    "LogLevel": {
      "Default": "Trace",
      "Microsoft.AspNetCore": "Trace"
    }
  },
  "AllowedHosts": "*",
  "ServiceConfigurationOptions": {
    "Port": 6000,
    "RefreshTimeInSeconds": 2,
    "TimeOutInSeconds": 5,
    "TimeOutCheckingTimeMs": 1000
  },
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://localhost:5294"
      },
      "Https": {
        "Url": "https://localhost:5295"
      }
    }
  }
}

```

### Client side

The client constructor looks like this

```

        public ServiceDiscoveryClient(
            ILogger<ServiceDiscoveryClient> logger,
            ClientConfigurationOptions options,
            IMulticastClient multicastClient,
            IEnumerable<IMetadataProvider> metadataProviders)

```

Where
- logger: Is the logging instance you are using for tracing. This library provides a ConsoleLogger.
- metadataProviders: List of metadata providers. Metadata providers are used every time the client registers information into the discovery server.
- options: Where the client options lie.
- multicastClient: multicast client that is used for the autodiscovery feature if configured.

If you are not using dependency injection, and you like default options, you would instance and start your discovery client like this:

```
        var options = new ClientConfigurationOptions();

        var client = new ServiceDiscoveryClient(
            new ConsoleLogger<ServiceDiscoveryClient>(),
            options,
            new MulticastClient(new ConsoleLogger<MulticastClient>),
            new List<IMetadataProvider>{new SystemInfoMetadataProvider()});

        client.Start();

```


Client options are the following:

```
        /// <summary>
        /// Gets or sets the listening port for the server.
        /// The client will used to make calls to.
        /// <remarks>By default is 6000</remarks>
        /// </summary>
        public int Port { get; set; } = 6000;

        /// <summary>
        /// Host name of ip of discovery server service.
        /// Client will used to make calls to it.
        /// If empty, then auto discover will be used automatically.
        /// </summary>
        public string DiscoveryServerHost { get; set; }

        /// <summary>
        /// Service address. This address is send to the discovery server as callback address.
        /// This is the address we are registering in the service discovery as telling others how to reach me.
        /// </summary>
        public string ServiceAddress { get; set; }

        /// <summary>
        /// Gets or sets the name of the service this client will register in the discovery server.
        /// </summary>
        public string ServiceName { get; set; } = "ServiceName";

        /// <summary>
        /// Gets or sets the port this service is listening to requests.
        /// </summary>
        public int CallbackPort { get; set; } = 6001;

        /// <summary>
        /// Auto discovery multicast group.
        /// If DiscoveryServerHost is empty. Then auto discovery is used.
        /// The client subscribes to this multicast group waiting for specific broadcasts coming from the server side.
        /// </summary>
        public string AutoDiscoverMulticastGroup { get; set; } = "224.0.0.100";

        /// <summary>
        /// Auto discovery multicast port.
        /// The client waits for messages coming from the server in this port, only if auto discovery is enabled.
        /// (See DiscoveryServerHost)
        /// </summary>
        public int AutoDiscoverPort { get; set; } = 5478;

        /// <summary>
        /// Default client scope. Similar to a tag, domain, or environment where this client is under.
        /// It allows you to group this client as part of a set of clients of different services.
        /// </summary>
        public string Scope { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether [use secure connection].
        /// If secure connection is true and certificate name is found in the machine.
        /// </summary>
        /// <value>
        ///   <c>true</c> if [use secure connection]; otherwise, <c>false</c>.
        /// </value>
        public bool UseSecureConnection { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether [allow invalid certificates] is enabled.
        /// If enabled, invalid certificates like self-signed or untrusted certificates will be accepted.
        /// By using an untrusted invalid certificate you are encrypting the communication from end 2 end
        /// but you will be not safe against a man in the middle attack.
        /// </summary>
        /// <value>
        ///   <c>true</c> if [allow invalid certificates]; otherwise, <c>false</c>.
        /// </value>
        /// <remarks>Configuring the SSL communication is always a difficult task.
        /// You need to create a proper certificate for the server part.
        /// As a rule of thumb the issuer name usually matches the machine name, or dns of the server machine, where the server is running,
        /// and the client should reach it using this dns and not the ip. Also the certification provider authority should be trusted by the client.
        /// For Self-signed certificates you could achieve this trust by adding server certificate to the Trusted authorities in the client side.</remarks>
        public bool AllowInvalidCertificates { get; set; } = true;
```

## API

### Blazor Server App API

Default server will be available at:

```

https://localhost:5295/

```

Default page show a Services tab where all registered services are shown. 
You will be able to sort, find and enable/disable services from this page. Enabling and disabling services is done in runtime.
The page automaticaly refreses on every change.


## Contributing

PRs accepted and I will be really greatfull for them.


## Pending

There are several things I haven't addressed yet.
- Create a DNS resolver logic. Current implemtentation allows you to ask discovery server for the list of services that matches your request. The idea of this feature
would be to make the server give you the "best" one, where the best should follow a configured logic. In other words, the server would potentially become a load balancer.
- prepare the BlazorServer app dockerization file. Currently learning about it.
- The Blazor server app is using default Visual studio styling and it would be nice to make it "different".
- The blazor server app has a initial page that is empty. I am still thinking what to put there.


## License

[MIT © Guillermo Alías](../LICENSE)
