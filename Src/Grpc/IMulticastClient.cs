using System;
using Microsoft.Extensions.Logging;

namespace Dilan.GrpcServiceDiscovery.Grpc
{
    public interface IMulticastClient
    {
        /// <summary>
        /// Event OnReceived that fires when UDP command is captured
        /// </summary>
        event EventHandler<MulticastData> DataReceived;

        /// <summary>
        /// Gets the receiving RxPort.
        /// </summary>
        int ReceivePort { get; }

        /// <summary>
        /// Gets a value indicating whether UDP socket is started
        /// </summary>
        bool Enabled { get; }

        /// <summary>
        /// Gets or sets the multicast group.
        /// </summary>
        string MulticastGroup { get; }

        /// <summary>
        /// Gets or sets the logger.
        /// </summary>
        ILogger<MulticastClient> Logger { get; set; }

        /// <summary>
        /// Start service. If Rx type selected this function will start reception thread
        /// </summary>
        bool StartService(int port, string multicastGroup, int ttl = 1);

        /// <summary>
        /// Stop Service. If Type= RX Reception thread will close
        /// </summary>
        void StopService();

        /// <summary>
        /// Join this multicast group on all available network interfaces.
        /// </summary>
        /// <param name="multicastGroup"></param>
        /// <param name="rxPort"></param>
        /// <param name="timeToLive"></param>
        bool JoinMulticastGroup(string multicastGroup, int rxPort, int timeToLive = 1);

        /// <summary>
        /// Send Data to destination Ip and Port
        /// </summary>
        /// <param name="data">Byte Array</param>
        /// <param name="address">Destination Ip</param>
        /// <param name="destinationPort">Destination Port</param>
        void Send(string data, string address, int destinationPort);

        /// <summary>
        /// Send Data to the configured multicast group. In order to use this you had to previously call JoinMulticastGroup.
        /// </summary>
        /// <param name="data">Byte Array</param>
        /// <param name="destinationPort">Destination Port</param>
        void Send(string data, int destinationPort);
    }
}