using System;
using System.Collections.Generic;
using DatadogStatsD.Protocol;
using DatadogStatsD.Telemetering;
using DatadogStatsD.Transport;

namespace DatadogStatsD.Metrics
{
    /// <summary>
    /// Represents any DogstatsD metric.
    /// </summary>
    public abstract class Metric : IDisposable
    {
        private readonly ITransport _transport;
        private readonly ITelemetry _telemetry;
        private readonly byte[] _metricNameBytes;
        private readonly double _sampleRate;
        private readonly byte[]? _sampleRateBytes;
        private readonly byte[] _tagsBytes;

        internal Metric(ITransport transport, ITelemetry telemetry, string metricName, double sampleRate,
            IList<string>? tags, bool includeSampleRate)
        {
            _transport = transport;
            _telemetry = telemetry;
            _metricNameBytes = DogStatsDSerializer.SerializeMetricName(metricName);
            _sampleRate = sampleRate;
            _sampleRateBytes = includeSampleRate ? DogStatsDSerializer.SerializeSampleRate(sampleRate) : null;
            _tagsBytes = DogStatsDSerializer.ValidateAndSerializeTags(tags);
        }

        /// <summary>
        /// Do nothing. Overriden by metric like <see cref="Count"/> or <see cref="Gauge"/> that use a timer.
        /// </summary>
        public virtual void Dispose()
        {
        }

        protected void Send(double value, byte[] typeBytes)
        {
            _telemetry.MetricSent();

            if (!Sampling.Sample(_sampleRate))
                return;

            var metricBytes = DogStatsDSerializer.SerializeMetric(_metricNameBytes, value, typeBytes,
                _sampleRateBytes, _tagsBytes);
            _transport.Send(metricBytes);
        }
    }
}