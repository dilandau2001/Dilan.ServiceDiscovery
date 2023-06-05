using System.Collections.Concurrent;
using Dilan.GrpcServiceDiscovery.Grpc;

namespace Dilan.GrpcServiceDiscovery.BlazorServer.Data
{
    public class ServiceDiscoveryService
    {
        private readonly ServiceDiscoveryServer _server;

        public ServiceDiscoveryService(
            ServiceDiscoveryServer server)
        {
            _server = server;
            _server.ServiceModelListChanged += ServerOnServiceModelListChanged;
            _server.Start();
        }

        /// <summary>
        /// Event that occurs when any part of the current list has changed.
        /// </summary>
        public event EventHandler? ServiceModelListChanged;
        
        /// <summary>
        /// Gets or sets the list of available services.
        /// </summary>
        public ConcurrentDictionary<string, ServiceModel> ServiceDictionary => _server.ServiceDictionary;

        /// <summary>
        /// Removes from the server all offline services.
        /// </summary>
        public void Clear()
        {
            var remove = _server.ServiceDictionary.Where(n => n.Value.HealthState == EnumServiceHealth.Offline);
            foreach (var kvp in remove)
            {
                _server.ServiceDictionary.TryRemove(kvp.Key, out _);
            }

            ServiceModelListChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Fires when server list changes.
        /// </summary>
        private void ServerOnServiceModelListChanged(object? sender, EventArgs e)
        {
            ServiceModelListChanged?.Invoke(sender, e);
        }
    }
}