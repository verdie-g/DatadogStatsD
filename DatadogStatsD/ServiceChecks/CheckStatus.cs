namespace DatadogStatsD.ServiceChecks
{
    /// <summary>
    /// Status of a service.
    /// </summary>
    public enum CheckStatus
    {
        /// <summary>
        /// Ok status.
        /// </summary>
        Ok,

        /// <summary>
        /// Warning status.
        /// </summary>
        Warning,

        /// <summary>
        /// Critical status.
        /// </summary>
        Critical,

        /// <summary>
        /// Unknown status.
        /// </summary>
        Unknown,
    }
}
