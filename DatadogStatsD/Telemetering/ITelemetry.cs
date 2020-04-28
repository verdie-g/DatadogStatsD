using System;

namespace DatadogStatsD.Telemetering
{
    internal interface ITelemetry : IDisposable
    {
        void MetricSent();
        void EventSent();
        void ServiceCheckSent();
        void PacketSent(int size);
        void PacketDropped(int size, bool queue);
    }

}