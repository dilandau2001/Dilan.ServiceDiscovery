using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

namespace Dilan.GrpcServiceDiscovery.Grpc
{
    /// <summary>
    /// Simplest metadata provider.
    /// It provides with the date the provider was created.
    /// </summary>
    public class SystemInfoMetadataProvider : IMetadataProvider
    {
        private Dictionary<string, string> _metadata;

        #region Implementation of IMetadataProvider

        /// <summary>
        /// Calculate metadata and retrieve it.
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, string> GetMetadata()
        {
            Process p = Process.GetCurrentProcess();

            if (_metadata == null)
            {  
                string cMyProcessName = p.ProcessName;

                _metadata = new Dictionary<string, string>
                {
                    { "StartTime", p.StartTime.ToString(CultureInfo.InvariantCulture)},
                    { "MachineName", Environment.MachineName },
                    { "UserName", Environment.UserName },
                    { "ProcessName", cMyProcessName }
                };
            }

            double memory = p.PrivateMemorySize64 / (1024.0f * 1024.0f);
            string s = $"{memory:N2}";
            _metadata["MemoryMb"] = s;

            return _metadata;
        }

        #endregion
    }
}
