using System.Collections.Generic;

namespace Dilan.GrpcServiceDiscovery.Grpc
{
    /// <summary>
    /// Describes an instance that retrieves some metadata in the form of dictionary of strings.
    /// </summary>
    public interface IMetadataProvider
    {
        /// <summary>
        /// Calculate metadata and retrieve it.
        /// </summary>
        /// <returns></returns>
        Dictionary<string, string> GetMetadata();
    }
}
