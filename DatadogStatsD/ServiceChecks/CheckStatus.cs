namespace DatadogStatsD.ServiceChecks
{
    /// <summary>
    /// Status of a service.
    /// </summary>
    public enum CheckStatus
    {
        Ok,
        Warning,
        Critical,
        Unknown,
    }
}