using System;
using System.Collections.Generic;
using System.Text;
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

        private readonly string _metricName;
        private readonly byte[] _metricNameBytes;
        private readonly double _sampleRate;
        private readonly byte[]? _sampleRateBytes;
        private readonly IList<string>? _tags;
        private readonly byte[] _tagsBytes;

        internal abstract MetricType MetricType { get; }

        internal Metric(ITransport transport, ITelemetry telemetry, string metricName, double sampleRate,
            IList<string>? tags, bool includeSampleRate)
        {
            _transport = transport;
            _telemetry = telemetry;
            _metricName = metricName;
            _metricNameBytes = DogStatsDSerializer.SerializeMetricName(metricName);
            _sampleRate = sampleRate;
            _sampleRateBytes = includeSampleRate ? DogStatsDSerializer.SerializeSampleRate(sampleRate) : null;
            _tags = tags;
            _tagsBytes = DogStatsDSerializer.ValidateAndSerializeTags(tags);
        }

        /// <summary>
        /// Do nothing. Overriden by metric like <see cref="Count"/> or <see cref="Gauge"/> that use a timer.
        /// </summary>
        public virtual void Dispose()
        {
        }

        /// <summary>
        /// Returns a string representing the <see cref="Metric"/>.
        /// </summary>
        public override string ToString()
        {
            var sb = new StringBuilder(_metricName);
            if (_tags == null || _tags.Count == 0)
            {
                return sb.ToString();
            }

            sb.Append("[");
            foreach (string tag in _tags)
            {
                sb.Append(tag + ",");
            }

            sb.Length -= 1; // remove last comma
            sb.Append("]");
            return sb.ToString();
        }

        internal void Send(double value)
        {
            _telemetry.MetricSent();

            if (!Sampling.Sample(_sampleRate))
                return;

            var metricBytes = DogStatsDSerializer.SerializeMetric(_metricNameBytes, value, MetricType,
                _sampleRateBytes, _tagsBytes);
            _transport.Send(metricBytes);
        }
    }
}