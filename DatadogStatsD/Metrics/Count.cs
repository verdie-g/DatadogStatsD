using System.Collections.Generic;
using DatadogStatsD.Transport;

namespace DatadogStatsD.Metrics
{
    /// <summary>
    /// The COUNT metric represents the total number of event occurrences in one time interval. A COUNT can be used to
    /// track the total number of connections made to a database or the total number of requests to an endpoint. This
    /// number of events can accumulate or decrease over time — it is not monotonically increasing.
    /// </summary>
    /// <remarks>Documentation: https://docs.datadoghq.com/developers/metrics/types?tab=count</remarks>
    public class Count : Metric
    {
        private static readonly byte[] TypeBytes = DogStatsDSerializer.SerializeMetricType(MetricType.Count);

        internal Count(ITransport transport, string metricName, double sampleRate, IList<string> tags)
            : base(transport, metricName, sampleRate, tags, true)
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