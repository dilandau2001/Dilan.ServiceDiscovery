using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Timers;
using Appccelerate.StateMachine;
using Appccelerate.StateMachine.AsyncMachine;
using Appccelerate.StateMachine.AsyncMachine.Events;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;

namespace Dilan.GrpcServiceDiscovery.Grpc
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class ServiceDiscoveryClient : IDisposable
    {
        private DiscoveryService.DiscoveryServiceClient _client;
        private AsyncPassiveStateMachine<States, Events> _machine;
        private readonly Timer _tempo;
        private States _currentState;
        private int _refreshTime;
        private readonly MulticastClient _multicastClient;
        private readonly IEnumerable<IMetadataProvider> _metadataProviders;

        public enum States
        {
            NotConnected,
            AutoDiscovering,
            Connecting,
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
            MulticastClient multicastClient,
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
        /// 
        /// </summary>
        /// <returns></returns>
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
        /// 
        /// </summary>
        /// <returns></returns>
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
        /// 
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
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
        /// 
        /// </summary>
        /// <param name="serviceName"></param>
        /// <param name="scope"></param>
        /// <returns></returns>
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
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TempoOnElapsed(object sender, ElapsedEventArgs e)
        {
            using (Logger.BeginScope(nameof(TempoOnElapsed)))
            {
                _machine.Fire(Events.TimerFired);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
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
        /// 
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

        private void AutoDiscoveringOnEnter()
        {
            State = States.AutoDiscovering;
            _multicastClient.StartService(Options.AutoDiscoverPort, Options.AutoDiscoverMulticastGroup);
        }
        
        private void MulticastClientOnDataReceived(object sender, MulticastData e)
        {
            var split = e.Message.Split(';');

            if (split.Length != 2 || !split[0].StartsWith("DiscoveryServerIp"))
                return;
            
            try
            {
                Options.DiscoveryServerHost = split[0].Split('=')[1];
                Options.Port = int.Parse(split[1].Split('=')[1]);
                _machine.Fire(Events.AutoDiscoveringFinished);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, exception.Message);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private async Task ConnectingOnEnter()
        {
            using (Logger.BeginScope(nameof(ConnectingOnEnter)))
            {
                State = States.Connecting;

                if (string.IsNullOrEmpty(Options.DiscoveryServerHost))
                {
                    await _machine.Fire(Events.AutoDiscoveringNeeded);
                    return;
                }

                var address = "http://" + Options.DiscoveryServerHost + ":" + Options.Port;
                var channel = GrpcChannel.ForAddress(
                    address,
                    new GrpcChannelOptions
                    {
                        Credentials = ChannelCredentials.Insecure
                    });
            
                _client = new DiscoveryService.DiscoveryServiceClient(channel);

                // Reset timer.
                _tempo.Stop();
                _tempo.Start();

                await ExecuteRegistration();
                _tempo.Start();
            }
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private Task NotConnectedOnEnter()
        {
            using (Logger.BeginScope(nameof(NotConnectedOnEnter)))
            {
                State = States.NotConnected;
                _tempo.Stop();
                return Task.CompletedTask;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private async Task ExecuteRegistration()
        {
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
        }

        
        private void MachineOnTransitionExceptionThrown(object sender, TransitionExceptionEventArgs<States, Events> e)
        {
            Logger.LogError(e.Exception, e.Exception.Message);
        }


        #region IDisposable

        /// <summary>
        /// 
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
