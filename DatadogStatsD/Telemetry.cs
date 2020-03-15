using System;
using System.Diagnostics;
using System.Reflection;
using System.Timers;
using DatadogStatsD.Metrics;
using DatadogStatsD.Transport;

namespace DatadogStatsD
{
    internal interface ITelemetry : IDisposable
    {
        void MetricSent();
        void EventSent();
        void ServiceCheckSent();
        void PacketSent(int size);
        void PacketDropped(int size, bool queue);
    }

    /// <remarks>https://docs.datadoghq.com/developers/dogstatsd/high_throughput#client-side-telemetry</remarks>
    internal class Telemetry : ITelemetry
    {
        private static readonly TimeSpan FlushInterval = TimeSpan.FromSeconds(10);

        private const string ClientTag = "client:cs";
        private const string ClientVersionKey = "client_version";
        private const string ClientTransportKey = "client_transport";

        /// <summary>
        /// Number of metrics sent (before sampling).
        /// </summary>
        private readonly Count _metricsCount;

        /// <summary>
        /// Number of events sent.
        /// </summary>
        private readonly Count _eventsCount;

        /// <summary>
        /// Number of service_checks sent.
        /// </summary>
        private readonly Count _serviceChecksCount;

        /// <summary>
        /// Number of bytes successfully sent to the Agent.
        /// </summary>
        private readonly Count _bytesSentCount;

        /// <summary>
        /// Number of bytes dropped.
        /// </summary>
        private readonly Count _bytesDroppedCount;

        /// <summary>
        /// Number of bytes dropped because the queue was full.
        /// </summary>
        private readonly Count _bytesDroppedQueueCount;

        /// <summary>
        /// Number of bytes dropped because of an error while writing.
        /// </summary>
        private readonly Count _bytesDroppedWriterCount;

        /// <summary>
        /// Number of datagrams successfully sent.
        /// </summary>
        private readonly Count _packetsSentCount;

        /// <summary>
        /// Number of datagrams dropped.
        /// </summary>
        private readonly Count _packetsDroppedCount;

        /// <summary>
        /// Number of datagrams dropped because the queue was full.
        /// </summary>
        private readonly Count _packetsDroppedQueueCount;

        /// <summary>
        /// Number of datagrams dropped because of an error while writing.
        /// </summary>
        private readonly Count _packetsDroppedWriterCount;

        public Telemetry(string transportName, ITransport transport, Timer tickTimer)
        {
            var tags = new[]
            {
                ClientTag,
                ClientVersionKey + ":" + FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion,
                ClientTransportKey + ":" + transportName,
            };

             _metricsCount = new Count(transport, this, tickTimer, "datadog.dogstatsd.client.metrics", tags);
             _eventsCount = new Count(transport, this, tickTimer, "datadog.dogstatsd.client.events", tags);
             _serviceChecksCount = new Count(transport, this, tickTimer, "datadog.dogstatsd.client.service_checks", tags);
             _bytesSentCount = new Count(transport, this, tickTimer, "datadog.dogstatsd.client.bytes_sent", tags);
             _bytesDroppedCount = new Count(transport, this, tickTimer, "datadog.dogstatsd.client.bytes_dropped", tags);
             _bytesDroppedQueueCount = new Count(transport, this, tickTimer, "datadog.dogstatsd.client.bytes_dropped_queue", tags);
             _bytesDroppedWriterCount = new Count(transport, this, tickTimer, "datadog.dogstatsd.client.bytes_dropped_writer", tags);
             _packetsSentCount = new Count(transport, this, tickTimer, "datadog.dogstatsd.client.packets_sent", tags);
             _packetsDroppedCount = new Count(transport, this, tickTimer, "datadog.dogstatsd.client.packets_dropped", tags);
             _packetsDroppedQueueCount = new Count(transport, this, tickTimer, "datadog.dogstatsd.client.packets_dropped_queue", tags);
             _packetsDroppedWriterCount = new Count(transport, this, tickTimer, "datadog.dogstatsd.client.packets_dropped_writer", tags);
        }

        public void MetricSent() => _metricsCount.Increment();
        public void EventSent() => _eventsCount.Increment();
        public void ServiceCheckSent() => _serviceChecksCount.Increment();

        public void PacketSent(int size)
        {
            _bytesSentCount.Increment(size);
            _packetsSentCount.Increment();
        }

        public void PacketDropped(int size, bool queue)
        {
            _bytesDroppedCount.Increment(size);
            _packetsDroppedCount.Increment();

            if (queue)
            {
                _bytesDroppedQueueCount.Increment(size);
                _packetsDroppedQueueCount.Increment();
            }
            else
            {
                _bytesDroppedWriterCount.Increment(size);
                _packetsDroppedWriterCount.Increment();
            }
        }

        public void Dispose()
        {
            _metricsCount.Dispose();
            _eventsCount.Dispose();
            _serviceChecksCount.Dispose();
            _bytesSentCount.Dispose();
            _bytesDroppedCount.Dispose();
            _bytesDroppedQueueCount.Dispose();
            _bytesDroppedWriterCount.Dispose();
            _packetsSentCount.Dispose();
            _packetsDroppedCount.Dispose();
            _packetsDroppedQueueCount.Dispose();
            _packetsDroppedWriterCount.Dispose();
        }
    }
}