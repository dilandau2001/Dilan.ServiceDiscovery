using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Timers;

namespace Dilan.GrpcServiceDiscovery.Grpc
{
    public sealed class ServerManagerLogic : IDisposable
    {
        private readonly ServiceConfigurationOptions _options;
        private readonly Timer _tempo;

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
        public void AddOrUpdate(ServiceDto dto)
        {
            if (dto == null)
            {
                return;
            }

            var id = GiveId(dto);
            ServiceDictionary.TryGetValue(id, out var model);

            if (model != null)
            {   
                AddOrUpdateContinueExists(dto, model);
            }
            else
            {   
                AddOrUpdateContinueDoesNotExists(dto);
            }

            ContinueUpdate();
        }

        /// <summary>
        /// Returns the list of services that are healthy.
        /// </summary>
        /// <param name="serviceName"></param>
        /// <param name="scope"></param>
        /// <returns></returns>
        public List<ServiceDto> FindService(string serviceName, string scope = "")
        {
            return ServiceDictionary
                .Values
                .Where(n=> scope == string.Empty || n.Scope!= null && scope != string.Empty && n.Scope.ToLower().Contains(scope.ToLower()))
                .Where(n=>n.ServiceName.ToLower().Contains(serviceName.ToLower()) && n.HealthState == EnumServiceHealth.Healthy && n.Enabled)
                .Select(n=> n.ToServiceDto())
                .ToList();
        }

        #region IDisposable

        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        public void Dispose()
        {
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
        private void AddOrUpdateContinueDoesNotExists(ServiceDto comingDto)
        {
            var model = new ServiceModel
            {
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
        }
        
        /// <summary>
        /// Occurs when the timer has to check for timed out items.
        /// </summary>
        private void TempoOnElapsed(object sender, ElapsedEventArgs e)
        {
            foreach (var item in ServiceDictionary.Values)
            {
                if (item.HealthState == EnumServiceHealth.Offline)
                    continue;

                if (item.TimeoutTime < DateTimeOffset.Now)
                {
                    item.HealthState = EnumServiceHealth.Offline;
                }
            }

            ContinueUpdate();
        }

        #endregion
    }
}
