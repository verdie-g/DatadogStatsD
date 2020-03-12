using System.Collections.Generic;
using DatadogStatsD.Transport;

namespace DatadogStatsD.Metrics
{
    /// <summary>
    /// <see cref="Count"/> tracks how many times something happened per second.
    /// </summary>
    /// <remarks>Documentation: https://docs.datadoghq.com/developers/metrics/types?tab=count#metric-types</remarks>
    public class Count : Metric
    {
        private static readonly byte[] TypeBytes = DogStatsDSerializer.SerializeMetricType(MetricType.Count);

        internal Count(ITransport transport, ITelemetry telemetry, string metricName, double sampleRate, IList<string>? tags)
            : base(transport, telemetry, metricName, sampleRate, tags, true)
        {
        }

        public void Increment(long delta = 1)
        {
            Send(delta, TypeBytes);
        }

        public void Decrement(long delta = 1)
        {
            Send(-delta, TypeBytes);
        }
    }
}