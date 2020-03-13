using System.Collections.Generic;
using DatadogStatsD.Transport;

namespace DatadogStatsD.Metrics
{
    /// <summary>
    /// <see cref="Distribution"/> tracks the statistical distribution of a set of values across your infrastructure.
    /// </summary>
    /// <remarks>Documentation: https://docs.datadoghq.com/developers/metrics/types?tab=distribution#metric-types</remarks>
    public class Distribution : Metric
    {
        private static readonly byte[] TypeBytes = DogStatsDSerializer.SerializeMetricType(MetricType.Distribution);

        internal Distribution(ITransport transport, ITelemetry telemetry, string metricName, double sampleRate, IList<string>? tags)
            : base(transport, telemetry, metricName, sampleRate, tags, false)
        {
        }

        public void Record(double value)
        {
            Send(value, TypeBytes);
        }
    }
}