namespace Dilan.GrpcServiceDiscovery.Grpc
{
    /// <summary>
    /// Service configuration parameters.
    /// </summary>
    public class ClientConfigurationOptions
    {
        public ClientConfigurationOptions()
        {
            string local = StaticHelpers.GetLocalIpAddress();

            if (local != string.Empty)
            {
                ServiceAddress = local;
            }
        }
        /// <summary>
        /// Gets or sets the listening port for the server.
        /// The client will used to make calls to.
        /// <remarks>By default is 6000</remarks>
        /// </summary>
        public int Port { get; set; } = 6000;

        /// <summary>
        /// Host name of ip of discovery server service.
        /// Client will used to make calls to it.
        /// </summary>
        public string DiscoveryServerHost { get; set; }

        /// <summary>
        /// Service address. This address is send to the discovery server as callback address.
        /// This is the address we are registering in the service discovery as telling others how to reach me.
        /// </summary>
        public string ServiceAddress { get; set; }

        /// <summary>
        /// Gets or sets the name of the service this client will register in the discovery server.
        /// </summary>
        public string ServiceName { get; set; } = "ServiceName";

        /// <summary>
        /// Gets or sets the port this service is listening to requests.
        /// </summary>
        public int CallbackPort { get; set; } = 6001;

        /// <summary>
        /// Auto discovery multicast group.
        /// If DiscoveryServerHost is empty. Then auto discovery is used.
        /// </summary>
        public string AutoDiscoverMulticastGroup { get; set; } = "224.0.0.100";

        /// <summary>
        /// Auto discovery multicast port.
        /// </summary>
        public int AutoDiscoverPort { get; set; } = 5478;

        /// <summary>
        /// Default client scope.
        /// </summary>
        public string Scope { get; set; }
    }
}
