using System;
using System.Collections.Generic;
using System.Timers;
using DatadogStatsD.Protocol;
using DatadogStatsD.Telemetering;
using DatadogStatsD.Transport;

namespace DatadogStatsD.Metrics
{
    /// <summary>
    /// <see cref="Gauge"/> measures the value of a metric at a particular time.
    /// </summary>
    /// <remarks>Documentation: https://docs.datadoghq.com/developers/metrics/types?tab=gauge#metric-types</remarks>
    public class Gauge : Metric
    {
        private static readonly byte[] TypeBytes = DogStatsDSerializer.SerializeMetricType(MetricType.Gauge);

        private readonly Timer _tickTimer;
        private readonly ElapsedEventHandler _onTick;

        internal Gauge(ITransport transport, ITelemetry telemetry, Timer tickTimer, string metricName,
            Func<double> evaluator, IList<string>? tags)
            : base(transport, telemetry, metricName, 1.0, tags, false)
        {
            _tickTimer = tickTimer;
            _onTick = (_, __) => Send(evaluator(), TypeBytes);
            _tickTimer.Elapsed += _onTick;
        }

        /// <summary>
        /// Break the bond between the <see cref="Count"/> and the <see cref="DogStatsD"/>. Not calling this method
        /// will result in CPU/Memory leak.
        /// </summary>
        public override void Dispose()
        {
            _tickTimer.Elapsed -= _onTick;
        }
    }
}