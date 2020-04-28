using System.Collections.Generic;
using System.Threading;
using System.Timers;
using DatadogStatsD.Telemetering;
using DatadogStatsD.Transport;
using Timer = System.Timers.Timer;

namespace DatadogStatsD.Metrics
{
    /// <summary>
    /// <see cref="Count"/> tracks how many times something happened per second.
    /// </summary>
    /// <remarks>Documentation: https://docs.datadoghq.com/developers/metrics/types?tab=count#metric-types</remarks>
    public class Count : Metric
    {
        private static readonly byte[] TypeBytes = DogStatsDSerializer.SerializeMetricType(MetricType.Count);

        private readonly Timer _tickTimer;
        private readonly ElapsedEventHandler _onTick;

        private long _value;

        internal Count(ITransport transport, ITelemetry telemetry, Timer tickTimer, string metricName, IList<string>? tags)
            : base(transport, telemetry, metricName, 1.0, tags, true)
        {
            _tickTimer = tickTimer;
            _onTick = (_, __) =>
            {
                long delta = Interlocked.Exchange(ref _value, 0);
                if (delta != 0)
                {
                    Send(delta, TypeBytes);
                }
            };
            _tickTimer.Elapsed += _onTick;
        }

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
            _tickTimer.Elapsed -= _onTick;
        }
    }
}