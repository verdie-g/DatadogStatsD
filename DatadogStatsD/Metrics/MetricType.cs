namespace DatadogStatsD.Metrics
{
    /// <summary>
    /// DogStatsD submission types.
    /// </summary>
    /// <remarks>Documentation: https://docs.datadoghq.com/developers/metrics/types</remarks>
    internal enum MetricType
    {
        Count,
        Distribution,
        Gauge,
        Histogram,
        Set,
    }
}