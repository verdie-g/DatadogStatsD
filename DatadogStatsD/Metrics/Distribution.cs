using System;
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
        internal Distribution(ITransport transport, ITelemetry telemetry, string metricName, double sampleRate, IList<KeyValuePair<string, string>>? tags)
            : base(transport, telemetry, metricName, MetricType.Distribution, sampleRate, tags)
        {
        }

        /// <summary>
        /// Record a global distribution value.
        /// </summary>
        /// <exception cref="ArgumentException"><paramref name="value"/> is NaN.</exception>
        public void Record(double value)
        {
            ThrowHelper.ThrowIfNaN(value);
            Submit(value);
        }
    }
}
