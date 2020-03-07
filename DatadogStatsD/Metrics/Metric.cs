using System.Collections.Generic;
using DatadogStatsD.Transport;

namespace DatadogStatsD.Metrics
{
    public abstract class Metric
    {
        private readonly ITransport _transport;
        private readonly byte[] _metricNameBytes;
        private readonly double _sampleRate;
        private readonly byte[]? _sampleRateBytes;
        private readonly byte[] _tagsBytes;

        protected Metric(ITransport transport, string metricName, double sampleRate, IList<string>? tags, bool includeSampleRate)
        {
            _transport = transport;
            _metricNameBytes = DogStatsDSerializer.SerializeMetricName(metricName);
            _sampleRate = sampleRate;
            _sampleRateBytes = includeSampleRate ? DogStatsDSerializer.SerializeSampleRate(sampleRate) : null;
            _tagsBytes = DogStatsDSerializer.SerializeTags(tags);
        }

        protected void Send(double value, byte[] typeBytes)
        {
            if (!Sampling.Sample(_sampleRate))
                return;

            var metricBytes = DogStatsDSerializer.SerializeMetric(_metricNameBytes, value, typeBytes,
                _sampleRateBytes, _tagsBytes);
            _transport.Send(metricBytes);
        }
    }
}