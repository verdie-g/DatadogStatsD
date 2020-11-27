using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace DatadogStatsD
{
    /// <summary>
    /// DogStatsD client configuration.
    /// </summary>
    /// <remarks>Documentation: https://docs.datadoghq.com/developers/dogstatsd#client-instantiation-parameters</remarks>
    public class DogStatsDConfiguration
    {
        /// <summary>
        /// The endpoint of the DogStatsD agent. Defaults to localhost:8125. Use one of those subclasses:
        /// <see cref="IPEndPoint"/>, <see cref="DnsEndPoint"/>, or <see cref="UnixDomainSocketEndPoint"/>.
        /// </summary>
        public EndPoint EndPoint { get; set; } = new DnsEndPoint("localhost", 8125);

        /// <summary>
        /// Namespace to prefix all metrics, and service checks.
        /// </summary>
        public string? Namespace { get; set; }

        /// <summary>
        /// Tags to apply to all metrics, events, and service checks. The value of a tag can be empty. These tags
        /// override the ones passed through the environment (DD_ENV, DD_SERVICE, DD_VERSION).
        /// </summary>
        public IList<KeyValuePair<string, string>>? ConstantTags { get; set; }

        /// <summary>
        /// Enabled telemetry. Defaults to true.
        /// </summary>
        public bool Telemetry { get; set; } = true;
    }
}
