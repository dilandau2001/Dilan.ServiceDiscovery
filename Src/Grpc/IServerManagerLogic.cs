using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Dilan.GrpcServiceDiscovery.Grpc
{
    public interface IServerManagerLogic : IDisposable
    {
        /// <summary>
        /// Event that occurs when any part of the current list has changed.
        /// </summary>
        event EventHandler ServiceModelListChanged;

        /// <summary>
        /// Gets or sets the list of available services.
        /// </summary>
        ConcurrentDictionary<string, ServiceModel> ServiceDictionary { get; }

        /// <summary>
        /// Add or updates service list.
        /// </summary>
        /// <param name="dto"></param>
        void AddOrUpdate(ServiceDto dto);

        /// <summary>
        /// Returns the list of services that are healthy.
        /// </summary>
        /// <param name="serviceName"></param>
        /// <param name="scope"></param>
        /// <returns></returns>
        List<ServiceDto> FindService(string serviceName, string scope = "");
    }
}