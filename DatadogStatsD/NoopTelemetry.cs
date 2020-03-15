namespace DatadogStatsD
{
    public class NoopTelemetry : ITelemetry
    {
        public void MetricSent()
        {
        }

        public void EventSent()
        {
        }

        public void ServiceCheckSent()
        {
        }

        public void PacketSent(int size)
        {
        }

        public void PacketDropped(int size, bool queue)
        {
        }

        public void Dispose()
        {
        }
    }
}