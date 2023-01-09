using Grpc.Core;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Timers;

namespace Dilan.GrpcServiceDiscovery.Grpc
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class ServiceDiscoveryServer : DiscoveryService.DiscoveryServiceBase, IDisposable
    {
        private readonly IServerManagerLogic _logic;
        private Server _server;
        private readonly IMulticastClient _client;
        private readonly Timer _tempo;

        /// <summary>
        /// Initialize a new instance of the ServiceDiscoveryServer class.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="logic"></param>
        /// <param name="options"></param>
        /// <param name="client"></param>
        public ServiceDiscoveryServer(
            ILogger<ServiceDiscoveryServer> logger,
            IServerManagerLogic logic,
            ServiceConfigurationOptions options,
            IMulticastClient client)
        {
            Logger = logger;
            Logger.BeginScope(nameof(ServiceDiscoveryServer));
            Options = options;
            _logic = logic;
            _client = client;
            _tempo = new Timer(TimeSpan.FromSeconds(options.AutoDiscoverFreq).TotalMilliseconds);
            _tempo.Elapsed += TempoOnElapsed;
            _logic.ServiceModelListChanged += LogicOnServiceModelListChanged;
        }

        /// <summary>
        /// Event that occurs when any part of the current list has changed.
        /// </summary>
        public event EventHandler ServiceModelListChanged;
        
        /// <summary>
        /// Gets or sets the list of available services.
        /// </summary>
        public ConcurrentDictionary<string, ServiceModel> ServiceDictionary => _logic.ServiceDictionary;

        /// <summary>
        /// Gets or sets the Logger.
        /// <remarks>I hardly tried to make this be auto injected by auto-fixture with no success.</remarks>
        /// </summary>
        public ILogger<ServiceDiscoveryServer> Logger { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public ServiceConfigurationOptions Options { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public void Start()
        {
            using (Logger.BeginScope(nameof(Start)))
            {
                if (_server != null)
                {
                    Logger.LogTrace("Service is already started and you need to stop if first.");
                    return;
                }

                var port = Options.Port;
                _server = new Server();
                _server.Services.Add(DiscoveryService.BindService(this)); 
                _server.Ports.Add(new ServerPort("0.0.0.0", port, ServerCredentials.Insecure)); 
                
                Logger.LogTrace("Starting service in port " + port);
                _server.Start();

                if (Options.EnableAutoDiscover)
                {
                    Logger.LogTrace("Auto discover is enabled");
                    _client.JoinMulticastGroup(Options.AutoDiscoverMulticastGroup, Options.AutoDiscoverPort);
                    _tempo.Interval = TimeSpan.FromSeconds(Options.AutoDiscoverFreq).TotalMilliseconds;
                    _tempo.Start();
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public async Task Stop()
        {
            using (Logger.BeginScope(nameof(Start)))
            {
                if (_server == null)
                    return;

                await _server.ShutdownAsync();
                _server = null;
                _tempo.Stop();
            }
        }

        #region Overrides of DiscoveryServiceBase

        /// <summary>
        /// Call to register a service.
        /// This must be called by server clients on start and repeatedly during operation.
        /// </summary>
        /// <param name="request">The request received from the client.</param>
        /// <param name="context">The context of the server-side call handler being invoked.</param>
        /// <returns>The response to send back to the client (wrapped by a task).</returns>
        public override Task<RegisterServiceResponse> RegisterService(ServiceDto request, ServerCallContext context)
        {
            using (Logger.BeginScope(nameof(RegisterService) + $"({request})"))
            {
                var response = new RegisterServiceResponse
                {
                    Ok = true,
                    RefreshRateSeconds = Options.RefreshTimeInSeconds
                };

                try
                {
                    _logic.AddOrUpdate(request);
                }
                catch (Exception e)
                {
                    response.Ok = false;
                    response.Error = e.Message;
                }

                Logger.LogDebug("Result is " + response);
                return Task.FromResult(response);  
            }
        }

        /// <summary>
        /// Call service to retrieve all services with service name.
        /// </summary>
        /// <param name="request">The request received from the client.</param>
        /// <param name="context">The context of the server-side call handler being invoked.</param>
        /// <returns>The response to send back to the client (wrapped by a task).</returns>
        public override Task<FindServiceResponse> FindService(FindServiceRequest request, ServerCallContext context)
        {
            using (Logger.BeginScope(nameof(FindService) + $"({request})"))
            {
                var response = new FindServiceResponse
                {
                    Ok = true
                };

                try
                {
                    var items = _logic.FindService(request.Name, request.Scope);
                    response.Services.AddRange(items);
                }
                catch (Exception e)
                {
                    response.Ok = false;
                    response.Error = e.Message;
                }

                Logger.LogDebug("Result is " + response);
                return Task.FromResult(response); 
            }
        }

        #endregion

        #region Private

        /// <summary>
        /// 
        /// </summary>
        private void TempoOnElapsed(object sender, ElapsedEventArgs e)
        {
            string mainIp = StaticHelpers.GetLocalIpAddress();
            string sms = $"DiscoveryServerIp={mainIp};Port={Options.Port}";
            _client.Send(sms, Options.AutoDiscoverPort);
        }

        /// <summary>
        /// 
        /// </summary>
        private void LogicOnServiceModelListChanged(object sender, EventArgs e)
        {
            ServiceModelListChanged?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region IDisposable

        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        public void Dispose()
        {
            _tempo.Elapsed -= TempoOnElapsed;
            _tempo.Stop();
            _tempo.Dispose();
            _logic?.Dispose();
            Stop().Wait();
        }

        #endregion
    }
}
