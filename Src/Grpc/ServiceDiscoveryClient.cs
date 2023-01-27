using Appccelerate.StateMachine;
using Appccelerate.StateMachine.AsyncMachine;
using Appccelerate.StateMachine.AsyncMachine.Events;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Timers;

namespace Dilan.GrpcServiceDiscovery.Grpc
{
    /// <summary>
    /// Instance of the client that tries to connect to the server and registers periodically.
    /// </summary>
    public sealed class ServiceDiscoveryClient : IDisposable
    {
        private DiscoveryService.DiscoveryServiceClient _client;
        private AsyncPassiveStateMachine<States, Events> _machine;
        private readonly Timer _tempo;
        private States _currentState;
        private int _refreshTime;
        private readonly IMulticastClient _multicastClient;
        private readonly IEnumerable<IMetadataProvider> _metadataProviders;
        private string _discoveryServerHost;
        private int _discoveryServerPort;
        private bool _discoveryFound;

        /// <summary>
        /// Possible client states
        /// </summary>
        public enum States
        {
            /// <summary>
            /// Initial, not started, not connected state.
            /// </summary>
            NotConnected,

            /// <summary>
            /// Auto discovering state. The client is waiting for multicast message in the configured group and port.
            /// </summary>
            AutoDiscovering,

            /// <summary>
            /// Connecting state. The client already knows where a server is and is trying to register in it.
            /// </summary>
            Connecting,

            /// <summary>
            /// Connected. The client was able to reach the server and it is already registered.
            /// </summary>
            Connected
        }

        private enum Events
        {
            ConnectionRequested,
            DisconnectionRequested,
            ConnectSuccessFull,
            ConnectionFailed,
            TimerFired,
            AutoDiscoveringNeeded,
            AutoDiscoveringFinished
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="options"></param>
        /// <param name="multicastClient"></param>
        /// <param name="metadataProviders"></param>
        public ServiceDiscoveryClient(
            ILogger<ServiceDiscoveryClient> logger,
            ClientConfigurationOptions options,
            IMulticastClient multicastClient,
            IEnumerable<IMetadataProvider> metadataProviders)
        {
            Logger = logger;
            Logger.BeginScope(nameof(ServiceDiscoveryClient));
            Options = options;
            _currentState = States.NotConnected;
            _multicastClient = multicastClient;
            _metadataProviders = metadataProviders;
            
            _tempo = new Timer(1000);
            _tempo.Elapsed += TempoOnElapsed;
            
            BuildGraph();

            _multicastClient.DataReceived += MulticastClientOnDataReceived;
            _machine.TransitionExceptionThrown += MachineOnTransitionExceptionThrown;
        }


        /// <summary>
        /// Gets or sets the Logger.
        /// <remarks>I hardly tried to make this be auto injected by auto fixture with no success.</remarks>
        /// </summary>
        public ILogger<ServiceDiscoveryClient> Logger { get; set; }

        /// <summary>
        /// Gets or sets the client configuration options.
        /// </summary>
        public ClientConfigurationOptions Options { get; set; }

        /// <summary>
        /// Gets the current state.
        /// </summary>
        public States State
        {
            get => _currentState;
            private set
            {
                if (_currentState != value)
                {
                    _currentState = value; 
                    Logger.LogTrace($"State changed to {value}");
                }
            }
        }

        /// <summary>
        /// Gets or sets the service health.
        /// </summary>
        public EnumServiceHealth ServiceHealth { get; set; } = EnumServiceHealth.Healthy;

        /// <summary>
        /// Client metadata.
        /// </summary>
        public Dictionary<string, string> ExtraData { get; } = new Dictionary<string, string>();

        /// <summary>
        /// Starts the client.
        /// </summary>
        public async Task Start()
        {
            using (Logger.BeginScope(nameof(Start)))
            {
                if (_machine.IsRunning)
                    return;

                await _machine.Start();

                if (_machine != null)
                {
                    await _machine.Fire(Events.ConnectionRequested);
                }
            }
        }

        /// <summary>
        /// Stops the client.
        /// </summary>
        public async Task Stop()
        {
            using (Logger.BeginScope(nameof(Stop)))
            {
                if (_machine== null || !_machine.IsRunning)
                    return;

                await _machine.Fire(Events.DisconnectionRequested);
                await _machine.Stop();
            }
        }

        /// <summary>
        /// Actively registers the service.
        /// </summary>
        /// <param name="dto">Registers the client into the server.</param>
        public async Task<RegisterServiceResponse> RegisterService(ServiceDto dto)
        {
            using (Logger.BeginScope(nameof(RegisterService) + $"({dto})"))
            {
                RegisterServiceResponse result;

                try
                {
                    result = await _client.RegisterServiceAsync(dto);
                }
                catch (Exception e)
                {
                    result = new RegisterServiceResponse
                    {
                        Error = e.Message
                    };
                }

                Logger.LogDebug("Result is " + result);
                return result;
            }
        }

        /// <summary>
        /// Find all services that matches the service name and the scope.
        /// </summary>
        /// <param name="serviceName">Service name or tag.</param>
        /// <param name="scope">Scope of the request. If set it means that we are requesting all services from those belonging to a specific scope.</param>
        public async Task<FindServiceResponse> FindService(string serviceName, string scope = "")
        {
            using (Logger.BeginScope(nameof(FindService) + $"({serviceName}, {scope})"))
            {
                FindServiceResponse result;
                try
                {
                    result = await _client.FindServiceAsync(new FindServiceRequest { Name = serviceName, Scope = scope});
                }
                catch (Exception e)
                {
                    result = new FindServiceResponse
                    {
                        Error = e.Message
                    };
                }

                Logger.LogDebug("Result is " + result);
                return result;
            }
        }

        /// <summary>
        /// Timer elapsed
        /// </summary>
        private void TempoOnElapsed(object sender, ElapsedEventArgs e)
        {
            using (Logger.BeginScope(nameof(TempoOnElapsed)))
            {
                _machine.Fire(Events.TimerFired);
            }
        }

        /// <summary>
        /// Calculate all info that is going to be registered into the server.
        /// </summary>
        /// <returns>Returns the service information to be sent.</returns>
        private ServiceDto CalculateServiceDto()
        {
            var dto = new ServiceDto
            {
                HealthState = ServiceHealth,
                ServicePort = Options.CallbackPort,
                ServiceName = Options.ServiceName,
                ServiceHost = Options.ServiceAddress,
                Scope = Options.Scope
            };
            
            dto.Metadata.Add(ExtraData);

            // Add data from metadata providers
            if (_metadataProviders != null)
            {
                foreach (var metadataProvider in _metadataProviders)
                {
                    var data = metadataProvider.GetMetadata();
                    dto.Metadata.Add(data);
                }
            }
            
            return dto;
        }

        /// <summary>
        /// State machine.
        /// </summary>
        private void BuildGraph()
        {
            using (Logger.BeginScope(nameof(BuildGraph)))
            {
                var builder = new StateMachineDefinitionBuilder<States, Events>();

                builder.In(States.NotConnected)
                    .ExecuteOnEntry(NotConnectedOnEnter)
                    .On(Events.ConnectionRequested).Goto(States.Connecting);

                builder.In(States.Connecting)
                    .ExecuteOnEntry(ConnectingOnEnter)
                    .On(Events.AutoDiscoveringNeeded).Goto(States.AutoDiscovering)
                    .On(Events.DisconnectionRequested).Goto(States.NotConnected)
                    .On(Events.TimerFired).Execute(ExecuteRegistration)
                    .On(Events.ConnectSuccessFull).Goto(States.Connected);

                builder.In(States.AutoDiscovering)
                    .ExecuteOnEntry(AutoDiscoveringOnEnter)
                    .ExecuteOnExit(AutoDiscoveringOnExit)
                    .On(Events.AutoDiscoveringFinished).Goto(States.Connecting)
                    .On(Events.DisconnectionRequested).Goto(States.NotConnected);

                builder.In(States.Connected)
                    .ExecuteOnEntry(ConnectedOnEnter)
                    .ExecuteOnExit(ConnectedOnExit)
                    .On(Events.DisconnectionRequested).Goto(States.NotConnected)
                    .On(Events.TimerFired).Execute(ExecuteRegistration)
                    .On(Events.ConnectionFailed).Goto(States.Connecting);

                builder.WithInitialState(States.NotConnected);
                var definition = builder.Build();
                _machine = definition.CreatePassiveStateMachine("DiscoveryService");
            }
        }

        private void AutoDiscoveringOnExit()
        {
            _multicastClient.StopService();
        }

        private async Task AutoDiscoveringOnEnter()
        {
            State = States.AutoDiscovering;
            bool success = _multicastClient.StartService(Options.AutoDiscoverPort, Options.AutoDiscoverMulticastGroup);

            // If unable to join a multicast group then auto-discover cannot work.
            if (!success)
            {
                Logger.LogError("Error. Automation was not able to join any multicast group for the autodiscovery feature. Is there any network allowing multicast?");
                await _machine.Fire(Events.AutoDiscoveringFinished);
            }
        }
        
        private void MulticastClientOnDataReceived(object sender, MulticastData e)
        {
            var split = e.Message.Split(';');

            if (split.Length != 2 || !split[0].StartsWith("DiscoveryServerIp"))
                return;
            
            try
            {
                _discoveryServerHost = e.Source.Address.ToString();
                _discoveryServerPort = int.Parse(split[1].Split('=')[1]);
                _discoveryFound = true;
                _machine.Fire(Events.AutoDiscoveringFinished);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, exception.Message);
            }
        }

        private Task ConnectedOnEnter()
        {
            using (Logger.BeginScope(nameof(ConnectedOnEnter)))
            {
                State = States.Connected;

                // Reset timer.
                _tempo.Stop();
                _tempo.Start();
                
                return Task.CompletedTask;
            }
        }

        private void ConnectedOnExit()
        {
            using (Logger.BeginScope(nameof(ConnectedOnExit)))
            {
                _discoveryFound = false;
            }
        }

        private async Task ConnectingOnEnter()
        {
            using (Logger.BeginScope(nameof(ConnectingOnEnter)))
            {
                State = States.Connecting;

                if (string.IsNullOrEmpty(Options.DiscoveryServerHost) && !_discoveryFound)
                {
                    await _machine.Fire(Events.AutoDiscoveringNeeded);
                    return;
                }

                string discoveryServerHost = _discoveryFound ? _discoveryServerHost : Options.DiscoveryServerHost;
                int discoveryServerPort = _discoveryFound ? _discoveryServerPort : Options.Port;
                
                if (Options.UseSecureConnection)
                {
                    var httpClientHandler = new HttpClientHandler();
                    
                    // Return `true` to allow certificates that are untrusted/invalid
                    httpClientHandler.ServerCertificateCustomValidationCallback = (message, certificate2, arg3, arg4) =>
                    {
                        if (Options.AllowInvalidCertificates)
                        {
                            return true;
                        }
                        else
                        {
                            Logger.LogError($"Invalid Certificate: {message}, {certificate2}, {arg3}, {arg4}");
                            return false;
                        }
                    };
                    
                    var httpClient = new HttpClient(httpClientHandler);

                    var address = "https://" + discoveryServerHost + ":" + discoveryServerPort;
                    var channel = GrpcChannel.ForAddress(
                        address,
                        new GrpcChannelOptions
                        {
                            Credentials = ChannelCredentials.SecureSsl,
                            HttpClient = httpClient
                        });
            
                    _client = new DiscoveryService.DiscoveryServiceClient(channel);
                }
                else
                {
                    var address = "http://" + discoveryServerHost + ":" + discoveryServerPort;
                    var channel = GrpcChannel.ForAddress(
                        address,
                        new GrpcChannelOptions
                        {
                            Credentials = ChannelCredentials.Insecure
                        });
            
                    _client = new DiscoveryService.DiscoveryServiceClient(channel);
                }

                

                await ExecuteRegistration();
            }
        }

        private Task NotConnectedOnEnter()
        {
            using (Logger.BeginScope(nameof(NotConnectedOnEnter)))
            {
                State = States.NotConnected;
                _tempo.Stop();
                return Task.CompletedTask;
            }
        }

        private async Task ExecuteRegistration()
        {
            // while registering stop the timer.
            _tempo.Stop();
            var dto = CalculateServiceDto();
            var result = await RegisterService(dto);

            if (result.Ok)
            {
                _refreshTime = result.RefreshRateSeconds;                
                await _machine.Fire(Events.ConnectSuccessFull);
            }
            else
            {
                _refreshTime = 5;
                await _machine.Fire(Events.ConnectionFailed);
            }

            _tempo.Interval = TimeSpan.FromSeconds(_refreshTime).TotalMilliseconds;
            _tempo.Start();
        }

        
        private void MachineOnTransitionExceptionThrown(object sender, TransitionExceptionEventArgs<States, Events> e)
        {
            Logger.LogError(e.Exception, e.Exception.Message);
        }


        #region IDisposable

        /// <summary>
        /// Dispose.
        /// </summary>
        public void Dispose()
        {
            Stop().Wait();
            _tempo.Elapsed -= TempoOnElapsed;
            _multicastClient.DataReceived -= MulticastClientOnDataReceived;
            _machine.TransitionExceptionThrown -= MachineOnTransitionExceptionThrown;
            _tempo?.Dispose();
        }

        #endregion
    }
}
