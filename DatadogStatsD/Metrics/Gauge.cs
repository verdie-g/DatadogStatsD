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
            : base(transport, telemetry, metricName, MetricType.Gauge, 1.0, tags)
        {
            _tickTimer = tickTimer;
            _evaluator = evaluator;
            _tickTimer.Elapsed += OnTick;
        }

        /// <summary>
        /// Break the bond between the <see cref="Count"/> and the <see cref="DogStatsD"/>. Not calling this method
        /// will result in CPU/Memory leak.
        /// </summary>
        public override void Dispose()
        {
            _tickTimer.Elapsed -= OnTick;
            OnTick(null, null); // flush
        }

        private void OnTick(object? _, ElapsedEventArgs? __)
        {
            double value;
            try
            {
                value = _evaluator();
            }
            catch
            {
                // Don't send anything if the evaluator threw.
                return;
            }

            Send(value);
        }
    }
}
