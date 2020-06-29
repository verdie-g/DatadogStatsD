using System;
using System.Collections.Generic;
using System.Timers;
using DatadogStatsD.Telemetering;
using DatadogStatsD.Ticking;
using DatadogStatsD.Transport;

namespace DatadogStatsD.Metrics
{
    /// <summary>
    /// <see cref="Gauge"/> measures the value of a metric at a particular time.
    /// </summary>
    /// <remarks>Documentation: https://docs.datadoghq.com/developers/metrics/types?tab=gauge#metric-types</remarks>
    public class Gauge : Metric
    {
        private readonly ITimer _tickTimer;
        private readonly Func<double> _evaluator;

        internal Gauge(ITransport transport, ITelemetry telemetry, ITimer tickTimer, string metricName,
            Func<double> evaluator, IList<string>? tags)
            : base(transport, telemetry, metricName, 1.0, tags, false)
        {
            _tickTimer = tickTimer;
            _evaluator = evaluator;
            _tickTimer.Elapsed += OnTick;
        }

        internal override MetricType MetricType => MetricType.Gauge;

        /// <summary>
        /// Break the bond between the <see cref="Count"/> and the <see cref="DogStatsD"/>. Not calling this method
        /// will result in CPU/Memory leak.
        /// </summary>
        public override void Dispose()
        {
            OnTick(null, null);
            _tickTimer.Elapsed -= OnTick;
        }

        private void OnTick(object? _, ElapsedEventArgs? __)
        {
            Send(_evaluator());
        }
    }
}