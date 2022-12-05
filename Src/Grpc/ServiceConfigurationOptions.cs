namespace Dilan.GrpcServiceDiscovery.Grpc
{
    /// <summary>
    /// Service configuration parameters.
    /// </summary>
    public class ServiceConfigurationOptions
    {
        /// <summary>
        /// Gets or sets the listening port for the server.
        /// <remarks>By default is 6000</remarks>
        /// </summary>
        public int Port { get; set; } = 6000;

        /// <summary>
        /// Gets or sets the refresh time passed to clients.
        /// This is the time the server will communicate the clients they have to refresh its status.
        /// <remarks>By default is 5 seconds</remarks>
        /// </summary>
        public int RefreshTimeInSeconds { get; set; } = 1;

        /// <summary>
        /// Gets or sets the time out in seconds.
        /// This is the time the server will wait for incoming registrations.
        /// If a client service fails to register in less than this time then the service is configured to offline.
        /// Note this time should be higher than the refresh time so the clients have plenty of time to even miss a few messages.
        /// </summary>
        public int TimeOutInSeconds { get; set; } = 5;

        /// <summary>
        /// Time in milliseconds for the timer that is checking if there are any service time out.
        /// </summary>
        public int TimeOutCheckingTimeMs { get; set; } = 1000;

        /// <summary>
        /// Enables auto discovery.
        /// If enabled the server will be sending a multicast messages to the AutoDiscoverMulticastGroup,
        /// with a frequency in seconds of AutoDiscoverFreq and to the port AutoDiscoverPort.
        /// Clients can capture this message to retrieve discovery service ip and port.
        /// </summary>
        public bool EnableAutoDiscover { get; set; } = true;

        /// <summary>
        /// Auto discovery multicast group.
        /// </summary>
        public string AutoDiscoverMulticastGroup { get; set; } = "224.0.0.100";

        /// <summary>
        /// Auto discovery multicast port.
        /// </summary>
        public int AutoDiscoverPort { get; set; } = 5478;

        /// <summary>
        /// Auto discovery send data frequency.
        /// </summary>
        public int AutoDiscoverFreq { get; set; } = 5;
    }
}
