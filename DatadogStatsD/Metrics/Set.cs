using System.Collections.Generic;
using DatadogStatsD.Transport;

namespace DatadogStatsD.Metrics
{
    /// <summary>
    /// <see cref="Set"/> counts the number of unique elements in a group.
    /// </summary>
    public class Set : Metric
    {
        private static readonly byte[] TypeBytes = DogStatsDSerializer.SerializeMetricType(MetricType.Set);

        internal Set(ITransport transport, ITelemetry telemetry, string metricName, IList<string>? tags)
            : base(transport, telemetry, metricName, 1.0, tags, false)
        {
        }

        /// <summary>
        /// Adds the specified value to the set.
        /// </summary>
        /// <param name="value">The value to add to the set.</param>
        public void Add(long value)
        {
            Send(value, TypeBytes);
        }
    }
}