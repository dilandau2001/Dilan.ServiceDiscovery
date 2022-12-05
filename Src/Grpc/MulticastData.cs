using System.Net;

namespace Dilan.GrpcServiceDiscovery.Grpc
{
    /// <summary>
    /// Event args for multicast received data.
    /// </summary>
    public class MulticastData
    {
        /// <summary>
        /// Gets or sets the message received.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets the information regarding the data source.
        /// </summary>
        public IPEndPoint Source { get; set; }
    }
}
