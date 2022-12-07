using System;
using System.Collections.Generic;
using Google.Protobuf.Collections;

namespace Dilan.GrpcServiceDiscovery.Grpc
{
    /// <summary>
    /// Structure of service model.
    /// </summary>
    public class ServiceModel
    {
        private string _serviceName;
        private int _port;
        private string _address;
        private EnumServiceHealth _state;
        private DateTimeOffset _startTime;
        private DateTimeOffset _lastTime;
        private DateTimeOffset _timeOutTime;
        private Dictionary<string, string> _metadata;
        private bool _enabled = true;
        private string _scope;

        /// <summary>
        /// Name of the registered service.
        /// </summary>
        public string ServiceName
        {
            get => _serviceName;
            set => UpdateDirty(value, ref _serviceName);
        }

        /// <summary>
        /// Port of the registered service.
        /// </summary>
        public int Port
        {
            get => _port;
            set => UpdateDirty(value, ref _port);
        }

        /// <summary>
        /// Address of the registered service.
        /// </summary>
        public string Address
        {
            get => _address;
            set => UpdateDirty(value, ref _address);
        }

        /// <summary>
        /// Service metadata.
        /// </summary>
        public Dictionary<string, string> Metadata 
        {
            get => _metadata;
            set => UpdateDirty(value, ref _metadata);
        }
        
        /// <summary>
        /// Heath state of the service
        /// </summary>
        public EnumServiceHealth HealthState
        {
            get => _state;
            set => UpdateDirty(value, ref _state);
        }

        /// <summary>
        /// Stores the time of first registration.
        /// </summary>
        public DateTimeOffset StartTime
        {
            get => _startTime;
            set => UpdateDirty(value, ref _startTime);
        }

        /// <summary>
        /// Stores the time where last refresh was done.
        /// </summary>
        public DateTimeOffset LastRefreshTime
        {
            get => _lastTime;
            set => UpdateDirty(value, ref _lastTime);
        }

        /// <summary>
        /// Stores the time when service will be put to offline if not refreshed.
        /// </summary>
        public DateTimeOffset TimeoutTime 
        {
            get => _timeOutTime;
            set => UpdateDirty(value, ref _timeOutTime);
        }

        /// <summary>
        /// Stores the time when service will be put to offline if not refreshed.
        /// </summary>
        public bool Enabled 
        {
            get => _enabled;
            set => UpdateDirty(value, ref _enabled);
        }

        /// <summary>
        /// Gets or sets the scope of the service. Ths is the environment the services belong to.
        /// </summary>
        public string Scope 
        {
            get => _scope;
            set => UpdateDirty(value, ref _scope);
        }

        /// <summary>
        /// True when a property is changed.
        /// </summary>
        public bool Dirty { get; set; }

        /// <summary>
        /// Updates value if it is different.
        /// And if so, sets dirty to true.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="newValue"></param>
        /// <param name="originalValue"></param>
        private void UpdateDirty<T>(T newValue, ref T originalValue)
        {
            if (newValue.Equals(originalValue))
                return;

            originalValue = newValue;
            Dirty = true;
        }

        /// <summary>
        /// Updates the metadata.
        /// Check metadata for changes.
        /// If changes, set dirty to true.
        /// </summary>
        /// <param name="metadata"></param>
        public void UpdateMetadata(MapField<string, string> metadata)
        {
            if (_metadata == null)
                _metadata = new Dictionary<string, string>();

            var setDirty = false;

            // if current metadata has different members of new metadata then we can flag dirty
            foreach (var keyValuePair in _metadata)
            {
                if (!metadata.ContainsKey(keyValuePair.Key))
                {
                    setDirty = true;
                    break;
                }
            }

            // new values
            foreach (var keyValuePair in metadata)
            {
                var found = _metadata.TryGetValue(keyValuePair.Key, out var existingValue);

                if (found && (existingValue == keyValuePair.Value))
                    continue;

                _metadata[keyValuePair.Key] = keyValuePair.Value;
                setDirty = true;
            }

            if (setDirty)
            {
                Dirty = true;
            }
        }

        public ServiceDto ToServiceDto()
        {
            return new ServiceDto
            {
                ServiceHost = _address,
                ServiceName = _serviceName,
                ServicePort = _port,
                HealthState = _state,
                Scope = _scope,
                Metadata = { _metadata }
            };
        }
    }
}
