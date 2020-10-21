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
        private readonly double _sampleRate;
        private readonly IList<string>? _tags;

        /// <summary>
        /// Everything before the value (namespace + name).
        /// </summary>
        private readonly byte[] _metricPrefixBytes;

        /// <summary>
        /// Everything after the value (type + sample rate + tags).
        /// </summary>
        private readonly byte[] _metricSuffixBytes;

        internal Metric(ITransport transport, ITelemetry telemetry, string metricName, MetricType metricType,
            double sampleRate, IList<string>? tags)
        {
            _transport = transport;
            _telemetry = telemetry;
            _metricName = metricName;
            _sampleRate = sampleRate;
            _tags = tags;

            _metricPrefixBytes = DogStatsDSerializer.SerializeMetricPrefix(metricName);
            _metricSuffixBytes = DogStatsDSerializer.SerializeMetricSuffix(metricType, sampleRate, tags);
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

            sb.Append('[');
#if NETSTANDARD2_0
            foreach (string tag in _tags)
            {
                sb.Append(tag);
                sb.Append(',');
            }

            // Remove last comma
            if (_tags.Count != 0)
            {
                sb.Length -= 1;
            }
#else
            sb.AppendJoin(',', _tags);
#endif
            sb.Append(']');
            return sb.ToString();
        }

        private protected void Send(double value)
        {
            _telemetry.MetricSent();

            if (!Sampling.Sample(_sampleRate))
                return;

            var metricBytes = DogStatsDSerializer.SerializeMetric(_metricPrefixBytes, value, _metricSuffixBytes);
            _transport.Send(metricBytes);
        }
    }
}
