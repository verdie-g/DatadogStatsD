namespace DatadogStatsD
{
    /// <summary>
    /// DogStatsD submission types.
    /// </summary>
    /// <remarks>Documentation: https://docs.datadoghq.com/developers/metrics/types</remarks>
    internal enum MetricType
    {
        Count,
        Rate,
        Gauge,
        Histogram,
        Distribution,
    }
}