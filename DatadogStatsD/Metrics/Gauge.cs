using System;
using System.Collections.Generic;
using System.Timers;
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
        private static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(10); // only the last value is kept
        private static readonly Timer TickTimer = new Timer(TickInterval.TotalMilliseconds) { Enabled = true };

        internal Gauge(ITransport transport, ITelemetry telemetry, string metricName, Func<double> evaluator, IList<string>? tags)
            : base(transport, telemetry, metricName, 1.0, tags, false)
        {
            TickTimer.Elapsed += (_, __) => Send(evaluator(), TypeBytes);
        }
    }
}