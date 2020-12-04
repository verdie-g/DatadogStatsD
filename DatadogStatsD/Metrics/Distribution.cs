using System.Collections.Generic;
using DatadogStatsD.Telemetering;
using DatadogStatsD.Transport;

namespace DatadogStatsD.Metrics
{
    /// <summary>
    /// <see cref="Distribution"/> tracks the statistical distribution of a set of values across your infrastructure.
    /// </summary>
    /// <remarks>Documentation: https://docs.datadoghq.com/developers/metrics/types?tab=distribution#metric-types</remarks>
    public class Distribution : Metric
    {
        internal Distribution(ITransport transport, ITelemetry telemetry, string metricName, IList<KeyValuePair<string, string>>? tags)
            : base(transport, telemetry, metricName, MetricType.Distribution, tags)
        {
        }

        /// <summary>
        /// Record a global distribution value.
        /// </summary>
        public void Record(double value)
        {
            Submit(value);
        }
    }
}
