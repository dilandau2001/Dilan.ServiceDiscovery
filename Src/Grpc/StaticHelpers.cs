using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Dilan.GrpcServiceDiscovery.Grpc
{
    /// <summary>
    /// Static functions that can be used our of context.
    /// </summary>
    public static class StaticHelpers
    {
        /// <summary>
        /// Returns all IPv4 ip addresses of on line network interfaces.
        /// </summary>
        /// <param name="_type">Network interface type. By default Ethernet.</param>
        /// <returns></returns>
        public static List<string> GetAllLocalIPv4(NetworkInterfaceType _type = NetworkInterfaceType.Ethernet)
        {
            var list = new List<string>();
            
            foreach (var item in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (item.NetworkInterfaceType != _type || item.OperationalStatus != OperationalStatus.Up)
                    continue;

                var address = item.GetIPProperties()
                    .UnicastAddresses
                    .Where(n=>n.Address.AddressFamily == AddressFamily.InterNetwork)
                    .Select(n=>n.Address.ToString())
                    .ToList();

                list.AddRange(address);
            }

            return list;
        }

        /// <summary>
        /// Returns a valid ip address among up interfaces.
        /// Excluding pseudo and virtual networks.
        /// </summary>
        /// <returns>Returns first ip address that is not virtual nor pseudo from all available up networks</returns>
        public static string GetLocalIpAddress()
        {
            var returnAddress = string.Empty;

            // Get a list of all network interfaces (usually one per network card, dial up, and VPN connection)
            var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

            foreach (var network in networkInterfaces)
            {
                // Read the IP configuration for each network
                var properties = network.GetIPProperties();

                if (network.OperationalStatus != OperationalStatus.Up ||
                    network.Description.ToLower().Contains("virtual") ||
                    network.Description.ToLower().Contains("pseudo"))
                    continue;
                
                // Each network interface may have multiple IP addresses
                foreach (var address in properties.UnicastAddresses.Select(n=>n.Address))
                {
                    // We're only interested in IPv4 addresses for now
                    if (address.AddressFamily != AddressFamily.InterNetwork)
                        continue;

                    // Ignore loopback addresses (e.g., 127.0.0.1)
                    if (IPAddress.IsLoopback(address))
                        continue;

                    returnAddress = address.ToString();
                    return returnAddress;
                }
            }

            return returnAddress;
        }

        /// <summary>
        /// Returns all valid ip address among up interfaces.
        /// Excluding pseudo and virtual networks.
        /// </summary>
        /// <returns></returns>
        public static List<string> GetAllLocalIpAddress()
        {
            List<string> result = new List<string>();

            // Get a list of all network interfaces (usually one per network card, dial up, and VPN connection)
            var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

            foreach (var network in networkInterfaces)
            {
                // Read the IP configuration for each network
                var properties = network.GetIPProperties();

                if (network.OperationalStatus != OperationalStatus.Up ||
                    network.Description.ToLower().Contains("virtual") ||
                    network.Description.ToLower().Contains("pseudo"))
                    continue;
                
                // Each network interface may have multiple IP addresses
                foreach (var address in properties.UnicastAddresses.Select(n=>n.Address))
                {
                    // We're only interested in IPv4 addresses for now
                    if (address.AddressFamily != AddressFamily.InterNetwork)
                        continue;

                    // Ignore loopback addresses (e.g., 127.0.0.1)
                    if (IPAddress.IsLoopback(address))
                        continue;

                    var returnAddress = address.ToString();
                    result.Add(returnAddress);
                }
            }

            return result;
        }

        public static int GetAvailablePort(int startingPort)
        {
            if (startingPort > ushort.MaxValue) throw new ArgumentException($"Can't be greater than {ushort.MaxValue}", nameof(startingPort));
            var ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();

            var connectionsEndpoints = ipGlobalProperties.GetActiveTcpConnections().Select(c => c.LocalEndPoint);
            var tcpListenersEndpoints = ipGlobalProperties.GetActiveTcpListeners();
            var udpListenersEndpoints = ipGlobalProperties.GetActiveUdpListeners();
            var portsInUse = connectionsEndpoints.Concat(tcpListenersEndpoints)
                .Concat(udpListenersEndpoints)
                .Select(e => e.Port);

            return Enumerable.Range(startingPort, ushort.MaxValue - startingPort + 1).Except(portsInUse).FirstOrDefault();
        }
    }
}
