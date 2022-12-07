using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

namespace Dilan.GrpcServiceDiscovery.Grpc
{
    /// <summary>
    /// Metadata providers that add basic information about the process running the client.
    /// </summary>
    public class SystemInfoMetadataProvider : IMetadataProvider
    {
        private Dictionary<string, string> _metadata;
        private TimeSpan _previousProcessorTime;
        private bool _first;
        private DateTime _time;

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

            // Add consumed ram in Mb.
            double memory = p.PrivateMemorySize64 / (1024.0f * 1024.0f);
            string s = $"{memory:N2}";
            _metadata["MemoryMb"] = s;

            // Add consumed CPU
            if (_first)
            {
                _time = DateTime.Now;
                _previousProcessorTime = p.TotalProcessorTime;
                _first = false;
                _metadata["Cpu"] = "0%";
            }
            else
            {
                var currentProcessorTime = p.TotalProcessorTime;
                var currentTime = DateTime.Now;
                var totalPassed = (currentTime - _time).TotalMilliseconds;
                var cpuUsed = (currentProcessorTime - _previousProcessorTime).TotalMilliseconds;
                var resultCpu = cpuUsed * 100 / (Environment.ProcessorCount * totalPassed);
                string cpu = $"{resultCpu:N2}%";
                _metadata["Cpu"] = cpu;

                _previousProcessorTime = currentProcessorTime;
                _time = currentTime;
            }


            return _metadata;
        }

        #endregion
    }
}
