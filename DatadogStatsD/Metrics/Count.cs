using System.Collections.Generic;
using System.Threading;
using System.Timers;
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
            _onTick = (_, __) => Send(Interlocked.Exchange(ref _value, 0), TypeBytes);
            _tickTimer.Elapsed += _onTick;
        }

        public void Increment(long delta = 1)
        {
            Interlocked.Add(ref _value, delta);
        }

        public void Decrement(long delta = 1)
        {
            Interlocked.Add(ref _value, -delta);
        }

        public override void Dispose()
        {
            _tickTimer.Elapsed -= _onTick;
        }
    }
}