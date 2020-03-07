using System.Collections.Generic;
using DatadogStatsD.Transport;

namespace DatadogStatsD.Metrics
{
    /// <summary>
    /// The HISTOGRAM metric represents the statistical distribution of a set of values calculated in one time interval.
    /// </summary>
    /// <remarks>Documentation: https://docs.datadoghq.com/developers/metrics/types/?tab=histogram#metric-types</remarks>
    public class Histogram : Metric
    {
        private static readonly byte[] TypeBytes = DogStatsDSerializer.SerializeMetricType(MetricType.Count);

        internal Histogram(ITransport transport, string metricName, double sampleRate, IList<string> tags)
            : base(transport, metricName, sampleRate, tags, true)
        {
        }

        public void Update(double value)
        {
            Send(value, TypeBytes);
        }
    }
}