using System.Collections.Generic;
using DatadogStatsD.Telemetering;
using DatadogStatsD.Transport;

namespace DatadogStatsD.Metrics
{
    /// <summary>
    /// <see cref="Set"/> counts the number of unique elements in a group.
    /// </summary>
    public class Set : Metric
    {
        internal Set(ITransport transport, ITelemetry telemetry, string metricName, IList<string>? tags)
            : base(transport, telemetry, metricName, MetricType.Set, 1.0, tags)
        {
        }

        /// <summary>
        /// Adds the specified value to the set.
        /// </summary>
        /// <param name="value">The value to add to the set.</param>
        public void Add(long value)
        {
            Send(value);
        }
    }
}
