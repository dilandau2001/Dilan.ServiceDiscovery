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
        /// If empty, then auto discover will be used automatically.
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
        /// The client subscribes to this multicast group waiting for specific broadcasts coming from the server side.
        /// </summary>
        public string AutoDiscoverMulticastGroup { get; set; } = "224.0.0.100";

        /// <summary>
        /// Auto discovery multicast port.
        /// The client waits for messages coming from the server in this port, only if auto discovery is enabled.
        /// (See DiscoveryServerHost)
        /// </summary>
        public int AutoDiscoverPort { get; set; } = 5478;

        /// <summary>
        /// Default client scope. Similar to a tag, domain, or environment where this client is under.
        /// It allows you to group this client as part of a set of clients of different services.
        /// </summary>
        public string Scope { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether [use secure connection].
        /// If secure connection is true and certificate name is found in the machine.
        /// </summary>
        /// <value>
        ///   <c>true</c> if [use secure connection]; otherwise, <c>false</c>.
        /// </value>
        public bool UseSecureConnection { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether [allow invalid certificates] is enabled.
        /// If enabled, invalid certificates like self-signed or untrusted certificates will be accepted.
        /// By using an untrusted invalid certificate you are encrypting the communication from end 2 end
        /// but you will be not safe against a man in the middle attack.
        /// </summary>
        /// <value>
        ///   <c>true</c> if [allow invalid certificates]; otherwise, <c>false</c>.
        /// </value>
        /// <remarks>Configuring the SSL communication is always a difficult task.
        /// You need to create a proper certificate for the server part.
        /// As a rule of thumb the issuer name usually matches the machine name, or dns of the server machine, where the server is running,
        /// and the client should reach it using this dns and not the ip. Also the certification provider authority should be trusted by the client.
        /// For Self-signed certificates you could achieve this trust by adding server certificate to the Trusted authorities in the client side.</remarks>
        public bool AllowInvalidCertificates { get; set; } = true;
    }
}
