namespace DatadogStatsD.Events
{
    /// <summary>
    /// Level of alert of an event.
    /// </summary>
    public enum AlertType
    {
        /// <summary>
        /// Info event.
        /// </summary>
        Info,

        /// <summary>
        /// Success event.
        /// </summary>
        Success,

        /// <summary>
        /// Warning event.
        /// </summary>
        Warning,

        /// <summary>
        /// Error event.
        /// </summary>
        Error,
    }
}
