using System.Collections.Generic;

namespace DatadogStatsD
{
    /// <remarks>Documentation: https://docs.datadoghq.com/developers/dogstatsd#client-instantiation-parameters</remarks>
    public class DogStatsDConfiguration
    {
        /// <summary>
        /// The host of your DogStatsD server.
        /// </summary>
        public string Host { get; set; }

        /// <summary>
        /// The port of your DogStatsD server.
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Tags to apply to all metrics, events, and service checks.
        /// </summary>
        public IList<string> ConstantTags { get; set; }

        /// <summary>
        /// Namespace to prefix all metrics, events, and service checks.
        /// </summary>
        public string Namespace { get; set; }
    }
}