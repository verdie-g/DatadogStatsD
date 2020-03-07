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
    public class Count
    {
        private static readonly byte[] TypeBytes = DogStatsDSerializer.SerializeMetricType(MetricType.Count);

        private readonly ITransport _transport;
        private readonly byte[] _metricNameBytes;
        private readonly double _sampleRate;
        private readonly byte[] _sampleRateBytes;
        private readonly byte[] _tagsBytes;

        internal Count(ITransport transport, string metricName, double sampleRate, IList<string> tags)
        {
            _transport = transport;
            _metricNameBytes = DogStatsDSerializer.SerializeMetricName(metricName);
            _sampleRate = sampleRate;
            _sampleRateBytes = DogStatsDSerializer.SerializeSampleRate(sampleRate);
            _tagsBytes = DogStatsDSerializer.SerializeTags(tags);
        }

        public void Increment(long delta = 1)
        {
            if (!Sampling.Sample(_sampleRate))
                return;

            var metricBytes = DogStatsDSerializer.SerializeMetric(_metricNameBytes, delta, TypeBytes,
                _sampleRateBytes, _tagsBytes);
            _transport.Send(metricBytes);
        }

        public void Decrement(long delta = 1)
        {
            if (!Sampling.Sample(_sampleRate))
                return;

            var metricBytes = DogStatsDSerializer.SerializeMetric(_metricNameBytes, -delta, TypeBytes,
                _sampleRateBytes, _tagsBytes);
            _transport.Send(metricBytes);
        }
    }
}