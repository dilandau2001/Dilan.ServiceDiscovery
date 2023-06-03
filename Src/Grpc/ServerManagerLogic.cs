using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Timers;

namespace Dilan.GrpcServiceDiscovery.Grpc
{
    /// <summary>
    /// Implementation of the server manager logic.
    /// </summary>
    public sealed class ServerManagerLogic : IServerManagerLogic
    {
        private readonly ServiceConfigurationOptions _options;
        private readonly Timer _tempo;

        /// <summary>
        /// Dictionary where key is tokenPassing group and value is service model identifier.
        /// Only one identifier per group can exist.
        /// </summary>
        private readonly ConcurrentDictionary<string, string> _tokenPassingDictionary = new ConcurrentDictionary<string, string>();

        /// <summary>
        /// Cached list of valid servers with service name as key.
        /// Always keep the list of valid services (enabled and healthy) given the service name.
        /// </summary>
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, ServiceModel>> _cachedValid =
            new ConcurrentDictionary<string, ConcurrentDictionary<string, ServiceModel>>();

        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the ServerManagerLogic class.
        /// </summary>
        public ServerManagerLogic(
            ServiceConfigurationOptions options)
        {
            _options = options;
            ServiceDictionary = new ConcurrentDictionary<string, ServiceModel>();
            _tempo = new Timer(options.TimeOutCheckingTimeMs);
            _tempo.Elapsed += TempoOnElapsed;
            _tempo.Start();
        }

        /// <summary>
        /// Event that occurs when any part of the current list has changed.
        /// </summary>
        public event EventHandler ServiceModelListChanged;
        
        /// <summary>
        /// Gets or sets the list of available services.
        /// </summary>
        public ConcurrentDictionary<string, ServiceModel> ServiceDictionary { get; }

        /// <summary>
        /// Add or updates service list.
        /// </summary>
        /// <param name="dto"></param>
        public ServiceModel AddOrUpdate(ServiceDto dto)
        {
            if (dto == null)
            {
                return null;
            }

            var id = GiveId(dto);
            ServiceDictionary.TryGetValue(id, out var model);

            if (model != null)
            {   
                AddOrUpdateContinueExists(dto, model);
            }
            else
            {   
                model = AddOrUpdateContinueDoesNotExists(dto, id);
            }

            EvaluatePrincipal(model, true);
            ContinueUpdate();
            return model;
        }

        /// <summary>
        /// Returns the list of services that are healthy.
        /// </summary>1
        /// <param name="serviceName"></param>
        /// <param name="scope"></param>
        /// <returns></returns>
        public List<ServiceModel> FindService(string serviceName, string scope = "")
        {
            var serviceNameToLower = serviceName.ToLower();

            _cachedValid.TryGetValue(serviceNameToLower, out ConcurrentDictionary<string, ServiceModel> initial);

            if (initial == null)
                return new List<ServiceModel>();
            
            if (string.IsNullOrEmpty(scope))
                return initial.Values.ToList();

            return initial.Values
                .Where(n=> n.Scope!= null && n.Scope.ToLower().Contains(scope.ToLower()))
                .ToList();
        }

        /// <summary>
        /// Forces a refresh.
        /// </summary>
        public void ForceRefresh()
        {
            foreach (ServiceModel item in ServiceDictionary.Values)
            {
                EvaluatePrincipal(item, false);

                if (item.HealthState == EnumServiceHealth.Offline)
                {
                    continue;
                }

                if (item.TimeoutTime < DateTimeOffset.Now)
                {
                    item.HealthState = EnumServiceHealth.Offline;
                }
            }

            ContinueUpdate();
        }

        #region IDisposable

        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        public void Dispose()
        {
            _disposed = true;
            _tempo.Stop();
            _tempo.Elapsed -= TempoOnElapsed;
        }

        #endregion

        #region Private

        /// <summary>
        /// From a service dto returns a string that should be unique for this service.
        /// Right now it is a combination of service name, ip and port.
        /// So the same service cannot be listening from the same ip at the same port.
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        private string GiveId(ServiceDto dto)
        {
            // We consider unique a combination of name, host and port.
            return $"({dto.ServiceName}-{dto.ServiceHost}-{dto.ServicePort})";
        }

        /// <summary>
        /// Checks if it is anything dirty in the list.
        /// If there is any, then raise event and clear dirties.
        /// </summary>
        private void ContinueUpdate()
        {
            // Is anything dirty
            var dirty = ServiceDictionary.Values.Where(n => n.Dirty).ToList();

            // If no changes
            if (!dirty.Any())
                return;
            
            // Raise
            ServiceModelListChanged?.Invoke(this, EventArgs.Empty);

            // Reset dirty property
            foreach (var serviceModel in dirty)
            {
                serviceModel.Dirty = false;
            }
        }

        /// <summary>
        /// Add or update when service already in the list.
        /// </summary>
        /// <param name="comingDto"></param>
        /// <param name="model"></param>
        private void AddOrUpdateContinueExists(ServiceDto comingDto, ServiceModel model)
        {
            model.ServiceName = comingDto.ServiceName;
            model.Address = comingDto.ServiceHost;
            model.Port = comingDto.ServicePort;
            model.HealthState = comingDto.HealthState;
            model.LastRefreshTime = DateTimeOffset.Now;
            model.TimeoutTime = DateTimeOffset.Now + TimeSpan.FromSeconds(_options.TimeOutInSeconds);
            model.Scope = comingDto.Scope;
            model.UpdateMetadata(comingDto.Metadata);
        }

        /// <summary>
        /// Add or update when service not in the list.
        /// </summary>
        /// <param name="comingDto"></param>
        /// <param name="id">Unique identifier of this service.</param>
        private ServiceModel AddOrUpdateContinueDoesNotExists(ServiceDto comingDto, string id)
        {
            var model = new ServiceModel
            {
                Id = id,
                ServiceName = comingDto.ServiceName,
                Address = comingDto.ServiceHost,
                Port = comingDto.ServicePort,
                HealthState = comingDto.HealthState,
                StartTime = DateTimeOffset.Now,
                LastRefreshTime = DateTimeOffset.Now,
                Scope = comingDto.Scope,
                TimeoutTime = DateTimeOffset.Now + TimeSpan.FromSeconds(_options.TimeOutInSeconds)
            };

            model.UpdateMetadata(comingDto.Metadata);

            ServiceDictionary.TryAdd(GiveId(comingDto), model);
            return model;
        }
        
        /// <summary>
        /// Occurs when the timer has to check for timed out items.
        /// </summary>
        private void TempoOnElapsed(object sender, ElapsedEventArgs e)
        {
            _tempo.Stop();
            ForceRefresh();

            if (!_disposed)
            {
                _tempo.Start();
            }
        }

        /// <summary>
        /// Decides if the service model should be the principal one among all services
        /// belonging to the same group. Only one service with the same ServiceName+Environment
        /// can be the principal one.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="allowPrincipalSet">Under the correct conditions allow setting Principal to true.</param>
        private void EvaluatePrincipal(ServiceModel model, bool allowPrincipalSet)
        {
            string groupName = model.TokenPassingGroupName;

            // if principal is no longer healthy or enabled.
            if ((!model.Enabled || model.HealthState != EnumServiceHealth.Healthy) &&
                model.Principal)
            {
                model.Principal = false;
                _tokenPassingDictionary.TryRemove(groupName, out _);
            }
            // if there is no principal and this service is valid, make it principal.
            else if (model.HealthState == EnumServiceHealth.Healthy &&
                     model.Enabled &&
                     allowPrincipalSet &&
                     !_tokenPassingDictionary.ContainsKey(groupName))
            {
                model.Principal = true;
                _tokenPassingDictionary.TryAdd(groupName, model.Id);
            }

            string serviceName = model.ServiceName.ToLower();
            if (!_cachedValid.ContainsKey(serviceName))
            {
                _cachedValid.TryAdd(serviceName, new ConcurrentDictionary<string, ServiceModel>());
            }

            // check if cache contains current model
            bool contains = _cachedValid[serviceName].ContainsKey(model.Id);

            if (model.Enabled &&
                model.HealthState == EnumServiceHealth.Healthy &&
                !contains)
            {
                _cachedValid[serviceName].TryAdd(model.Id, model);
            }
            else if (contains && (!model.Enabled || model.HealthState != EnumServiceHealth.Healthy))
            {
                _cachedValid[serviceName].TryRemove(model.Id, out _);
            }
        }

        #endregion
    }
}
