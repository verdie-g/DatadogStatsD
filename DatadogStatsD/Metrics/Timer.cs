using System.Collections.Generic;
using System.Diagnostics;
using DatadogStatsD.Transport;

namespace DatadogStatsD.Metrics
{
    /// <summary>
    /// <see cref="Timer"/> sends timing information.
    /// </summary>
    public class Timer : Metric
    {
        private static readonly byte[] TypeBytes = DogStatsDSerializer.SerializeMetricType(MetricType.Timer);

        internal Timer(ITransport transport, string metricName, double sampleRate, IList<string>? tags)
            : base(transport, metricName, sampleRate, tags, true)
        {
        }

        /// <summary>
        /// Sends <paramref name="stopwatch"/>.ElapsedMilliseconds.
        /// </summary>
        /// <param name="stopwatch"></param>
        public void Record(Stopwatch stopwatch)
        {
            Record(stopwatch.ElapsedMilliseconds);
        }

        /// <summary>
        /// Sends a milliseconds timing.
        /// </summary>
        /// <param name="timingMs"></param>
        public void Record(long timingMs)
        {
            Send(timingMs, TypeBytes);
        }
    }
}