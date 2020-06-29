using System.Collections.Generic;
using System.Threading;
using System.Timers;
using DatadogStatsD.Telemetering;
using DatadogStatsD.Ticking;
using DatadogStatsD.Transport;

namespace DatadogStatsD.Metrics
{
    /// <summary>
    /// <see cref="Count"/> tracks how many times something happened per second.
    /// </summary>
    /// <remarks>Documentation: https://docs.datadoghq.com/developers/metrics/types?tab=count#metric-types</remarks>
    public class Count : Metric
    {
        private readonly ITimer _tickTimer;

        private long _value;

        internal Count(ITransport transport, ITelemetry telemetry, ITimer tickTimer, string metricName, IList<string>? tags)
            : base(transport, telemetry, metricName, 1.0, tags, true)
        {
            _tickTimer = tickTimer;
            _tickTimer.Elapsed += OnTick;
        }

        internal override MetricType MetricType => MetricType.Count;

        /// <summary>
        /// Increment the <see cref="Count"/>.
        /// </summary>
        /// <param name="delta">Delta to add to the <see cref="Count"/>.</param>
        public void Increment(long delta = 1)
        {
            Interlocked.Add(ref _value, delta);
        }

        /// <summary>
        /// Decrement the <see cref="Count"/>.
        /// </summary>
        /// <param name="delta">Delta to subtract to the <see cref="Count"/>.</param>
        public void Decrement(long delta = 1)
        {
            Interlocked.Add(ref _value, -delta);
        }

        /// <summary>
        /// Break the bond between the <see cref="Count"/> and the <see cref="DogStatsD"/>. Not calling this method
        /// will result in CPU/Memory leak.
        /// </summary>
        public override void Dispose()
        {
            OnTick(null, null); // flush
            _tickTimer.Elapsed -= OnTick;
        }

        private void OnTick(object? _, ElapsedEventArgs? __)
        {
            long delta = Interlocked.Exchange(ref _value, 0);
            if (delta != 0)
            {
                Send(delta);
            }
        }
    }
}