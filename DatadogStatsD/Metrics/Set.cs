using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Timers;
using DatadogStatsD.Telemetering;
using DatadogStatsD.Ticking;
using DatadogStatsD.Transport;

namespace DatadogStatsD.Metrics
{
    /// <summary>
    /// <see cref="Set"/> counts the number of unique elements in a group.
    /// </summary>
    public class Set : Metric
    {
        private readonly ITimer _tickTimer;

        /// <summary>
        /// Aggregation of unique values between two ticks.
        /// </summary>
        /// <remarks>ConcurrentDictionary used as a concurrent set.</remarks>
        private ConcurrentDictionary<long, bool> _values;

        internal Set(ITransport transport, ITelemetry telemetry, ITimer tickTimer, string metricName, IList<string>? tags)
            : base(transport, telemetry, metricName, MetricType.Set, 1.0, tags)
        {
            _tickTimer = tickTimer;
            _values = new ConcurrentDictionary<long, bool>();

            _tickTimer.Elapsed += OnTick;
        }

        /// <summary>
        /// Adds the specified value to the set.
        /// </summary>
        /// <param name="value">The value to add to the set.</param>
        public void Add(long value) => _values[value] = true;

        /// <summary>
        /// Break the bond between the <see cref="Set"/> and the <see cref="DogStatsD"/>. Not calling this method
        /// will result in CPU/Memory leak.
        /// </summary>
        public override void Dispose()
        {
            _tickTimer.Elapsed -= OnTick;
            OnTick(null, null); // flush
        }

        private void OnTick(object? _, ElapsedEventArgs? __)
        {
            var values = Interlocked.Exchange(ref _values, new ConcurrentDictionary<long, bool>());
            foreach (long value in values.Keys)
            {
                Send(value);
            }
        }
    }
}
