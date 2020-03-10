using System.Collections.Generic;
using System.Diagnostics;
using DatadogStatsD.Transport;

namespace DatadogStatsD.Metrics
{
    public class Timer : Metric
    {
        private static readonly byte[] TypeBytes = DogStatsDSerializer.SerializeMetricType(MetricType.Timer);

        internal Timer(ITransport transport, string metricName, double sampleRate, IList<string>? tags)
            : base(transport, metricName, sampleRate, tags, true)
        {
        }

        public void Record(Stopwatch stopwatch)
        {
            Record(stopwatch.ElapsedMilliseconds);
        }

        public void Record(long value)
        {
            Send(value, TypeBytes);
        }
    }
}