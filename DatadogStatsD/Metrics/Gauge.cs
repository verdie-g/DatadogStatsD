using System;
using System.Collections.Generic;
using System.Threading;
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
        private readonly Func<double>? _evaluator;

        private double _value = double.NaN; // NaN indicates that no value was recorded.

        internal Gauge(ITransport transport, ITelemetry telemetry, ITimer tickTimer, string metricName,
            Func<double>? evaluator, IList<KeyValuePair<string, string>>? tags)
            : base(transport, telemetry, metricName, MetricType.Gauge, 1.0, tags)
        {
            _tickTimer = tickTimer;
            _evaluator = evaluator;
            _tickTimer.Elapsed += OnTick;
        }

        /// <summary>
        /// Updates the value of the current <see cref="Gauge"/> instance. The value will be used for the next datapoint
        /// of this gauge over the one returned by the evaluator function.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <exception cref="ArgumentException"><paramref name="value"/> is NaN.</exception>
        public void Update(double value)
        {
            ThrowHelper.ThrowIfNaN(value);
            Interlocked.Exchange(ref _value, value);
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
            double value = Interlocked.Exchange(ref _value, double.NaN);
            if (!double.IsNaN(value)) // If a value was recorded (using Record) since the last tick
            {
                Submit(value);
                return;
            }

            if (_evaluator == null)
            {
                // Don't send anything if no evaluator was specified and the Record method wasn't called
                return;
            }

            try
            {
                value = _evaluator();
            }
            catch
            {
                // Don't send anything if the evaluator threw.
                return;
            }

            Submit(value);
        }
    }
}
