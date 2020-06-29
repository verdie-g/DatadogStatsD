using System;
using System.Collections.Generic;
using DatadogStatsD.Telemetering;
using DatadogStatsD.Transport;

namespace DatadogStatsD.Metrics
{
    /// <summary>
    /// <see cref="Histogram"/> tracks the statistical distribution of a set of values on each host.
    /// </summary>
    /// <remarks>Documentation: https://docs.datadoghq.com/developers/metrics/types/?tab=histogram#metric-types</remarks>
    public class Histogram : Metric
    {
        internal Histogram(ITransport transport, ITelemetry telemetry, string metricName, double sampleRate, IList<string>? tags)
            : base(transport, telemetry, metricName, sampleRate, tags)
        {
        }

        internal override MetricType MetricType => MetricType.Histogram;

        [Obsolete("Use Sample instead")]
        public void Record(double value)
        {
            Send(value);
        }

        /// <summary>
        /// Samples a new value for current <see cref="Histogram"/> instance.
        /// </summary>
        /// <param name="value">The value.</param>
        public void Sample(double value)
        {
            Send(value);
        }
    }
}