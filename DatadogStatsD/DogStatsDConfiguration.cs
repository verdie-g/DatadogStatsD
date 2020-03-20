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
        public int Port { get; set; } = 8125;

        /// <summary>
        /// The path to the DogStatsD Unix domain socket (overrides <see cref="Host"/> and <see cref="Port"/>).
        /// </summary>
        public string? UnixSocketPath { get; set; }

        /// <summary>
        /// Tags to apply to all metrics, events, and service checks.
        /// </summary>
        public IList<string>? ConstantTags { get; set; }

        /// <summary>
        /// Namespace to prefix all metrics, and service checks.
        /// </summary>
        public string? Namespace { get; set; }

        /// <summary>
        /// Enabled telemetry. Defaults to true.
        /// </summary>
        public bool Telemetry { get; set; } = true;

        /// <summary>
        /// Source to use for events.
        /// </summary>
        public string? Source { get; set; }
    }
}