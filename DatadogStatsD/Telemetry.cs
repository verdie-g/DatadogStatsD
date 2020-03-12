using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using DatadogStatsD.Metrics;
using DatadogStatsD.Transport;
using Timer = System.Threading.Timer;

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

        private readonly Count _metricsCount;
        private readonly Count _eventsCount;
        private readonly Count _serviceChecksCount;
        private readonly Count _bytesSentCount;
        private readonly Count _bytesDroppedCount;
        private readonly Count _bytesDroppedQueueCount;
        private readonly Count _bytesDroppedWriterCount;
        private readonly Count _packetsSentCount;
        private readonly Count _packetsDroppedCount;
        private readonly Count _packetsDroppedQueueCount;
        private readonly Count _packetsDroppedWriterCount;

        private readonly Timer _flushTimer;

        /// <summary>
        /// Number of metrics sent (before sampling).
        /// </summary>
        private int _metrics;

        /// <summary>
        /// Number of events sent.
        /// </summary>
        private int _events;

        /// <summary>
        /// Number of service_checks sent.
        /// </summary>
        private int _serviceChecks;

        /// <summary>
        /// Number of bytes successfully sent to the Agent.
        /// </summary>
        private int _bytesSent;

        /// <summary>
        /// Number of bytes dropped.
        /// </summary>
        private int _bytesDropped;

        /// <summary>
        /// Number of bytes dropped because the queue was full.
        /// </summary>
        private int _bytesDroppedQueue;

        /// <summary>
        /// Number of bytes dropped because of an error while writing.
        /// </summary>
        private int _bytesDroppedWriter;

        /// <summary>
        /// Number of datagrams successfully sent.
        /// </summary>
        private int _packetsSent;

        /// <summary>
        /// Number of datagrams dropped.
        /// </summary>
        private int _packetsDropped;

        /// <summary>
        /// Number of datagrams dropped because the queue was full.
        /// </summary>
        private int _packetsDroppedQueue;

        /// <summary>
        /// Number of datagrams dropped because of an error while writing.
        /// </summary>
        private int _packetsDroppedWriter;

        public Telemetry(string transportName, ITransport transport)
        {
            var tags = new[]
            {
                ClientTag,
                ClientVersionKey + ":" + FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion,
                ClientTransportKey + ":" + transportName,
            };

             _metricsCount = new Count(transport, this, "datadog.dogstatsd.client.metrics", 1.0, tags);
             _eventsCount = new Count(transport, this, "datadog.dogstatsd.client.events", 1.0, tags);
             _serviceChecksCount = new Count(transport, this, "datadog.dogstatsd.client.service_checks", 1.0, tags);
             _bytesSentCount = new Count(transport, this, "datadog.dogstatsd.client.bytes_sent", 1.0, tags);
             _bytesDroppedCount = new Count(transport, this, "datadog.dogstatsd.client.bytes_dropped", 1.0, tags);
             _bytesDroppedQueueCount = new Count(transport, this, "datadog.dogstatsd.client.bytes_dropped_queue", 1.0, tags);
             _bytesDroppedWriterCount = new Count(transport, this, "datadog.dogstatsd.client.bytes_dropped_writer", 1.0, tags);
             _packetsSentCount = new Count(transport, this, "datadog.dogstatsd.client.packets_sent", 1.0, tags);
             _packetsDroppedCount = new Count(transport, this, "datadog.dogstatsd.client.packets_dropped", 1.0, tags);
             _packetsDroppedQueueCount = new Count(transport, this, "datadog.dogstatsd.client.packets_dropped_queue", 1.0, tags);
             _packetsDroppedWriterCount = new Count(transport, this, "datadog.dogstatsd.client.packets_dropped_writer", 1.0, tags);

             _flushTimer = new Timer(Flush, null, FlushInterval, FlushInterval);
        }

        public void MetricSent() => Interlocked.Increment(ref _metrics);
        public void EventSent() => Interlocked.Increment(ref _events);
        public void ServiceCheckSent() => Interlocked.Increment(ref _serviceChecks);

        public void PacketSent(int size)
        {
            Interlocked.Add(ref _bytesSent, size);
            Interlocked.Increment(ref _packetsSent);
        }

        public void PacketDropped(int size, bool queue)
        {
            Interlocked.Add(ref _bytesDropped, size);
            Interlocked.Increment(ref _packetsDropped);

            if (queue)
            {
                Interlocked.Add(ref _bytesDroppedQueue, size);
                Interlocked.Increment(ref _packetsDroppedQueue);
            }
            else
            {
                Interlocked.Add(ref _bytesDroppedWriter, size);
                Interlocked.Increment(ref _packetsDroppedWriter);
            }
        }

        public void Dispose()
        {
            _flushTimer.Dispose();
        }

        private void Flush(object _)
        {
            _metricsCount.Increment(Interlocked.Exchange(ref _metrics, 0));
            _eventsCount.Increment(Interlocked.Exchange(ref _events, 0));
            _serviceChecksCount.Increment(Interlocked.Exchange(ref _serviceChecks, 0));
            _bytesSentCount.Increment(Interlocked.Exchange(ref _bytesSent, 0));
            _bytesDroppedCount.Increment(Interlocked.Exchange(ref _bytesDropped, 0));
            _bytesDroppedQueueCount.Increment(Interlocked.Exchange(ref _bytesDroppedQueue, 0));
            _bytesDroppedWriterCount.Increment(Interlocked.Exchange(ref _bytesDroppedWriter, 0));
            _packetsSentCount.Increment(Interlocked.Exchange(ref _packetsSent, 0));
            _packetsDroppedCount.Increment(Interlocked.Exchange(ref _packetsDropped, 0));
            _packetsDroppedQueueCount.Increment(Interlocked.Exchange(ref _packetsDroppedQueue, 0));
            _packetsDroppedWriterCount.Increment(Interlocked.Exchange(ref _packetsDroppedWriter, 0));
        }
    }
}