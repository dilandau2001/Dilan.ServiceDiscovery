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
        /// <remarks>By default is 1 second</remarks>
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

        /// <summary>
        /// Gets or sets a value indicating whether [use secure connection].
        /// If secure connection is true and certificate name is found in the machine.
        /// </summary>
        /// <value>
        ///   <c>true</c> if [use secure connection]; otherwise, <c>false</c>.
        /// </value>
        /// <remarks>If security is enable you won't be able to communicate 2 full framework apps. 6-6 ok, and 6-4.8 ok, but 4.8-4.8 not ok.</remarks>
        public bool UseSecureConnection { get; set; } = true;

        /// <summary>
        /// Gets or sets the name of the certificate issuer name.
        /// </summary>
        /// <value>
        /// The name of the certificate issuer name.
        /// </value>
        public string CertificateIssuerName { get; set; } = "dilan.ServiceDiscovery";

        /// <summary>
        /// Gets or sets a value indicating whether [use certificate file].
        /// If this setting is set to true and UseSecureConnection is true then the Certificate file
        /// is searched inside the application folder.
        /// If this setting is false, then the certificate is searched in the Computer certificate repository.
        /// (In windows the Manage Workstation Certificates)
        /// </summary>
        /// <value>
        ///   <c>true</c> if [user certificate file]; otherwise, <c>false</c>.
        /// </value>
        public bool UseCertificateFile { get; set; } = false;

        /// <summary>
        /// Gets or sets the use certificate file password.
        /// When UseCertificateFile is used, in order to open the certificate file name.pfx you need
        /// to pass the password in order to get the private key.
        /// </summary>
        /// <value>
        /// The use certificate file password.
        /// </value>
        public string UseCertificateFilePassword { get; set; } = "dilandau2001";

    }
}
