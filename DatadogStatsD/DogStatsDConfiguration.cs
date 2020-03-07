using System.Collections.Generic;

namespace DatadogStatsD
{
    /// <remarks>Documentation: https://docs.datadoghq.com/developers/dogstatsd#client-instantiation-parameters</remarks>
    public class DogStatsDConfiguration
    {
        /// <summary>
        /// The host of your DogStatsD server. Defaults to "localhost".
        /// </summary>
        public string Host { get; set; } = "localhost";

        /// <summary>
        /// The port of your DogStatsD server. Defaults to 8125.
        /// </summary>
        public int? Port { get; set; } = 8125;

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