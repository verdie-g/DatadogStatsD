using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using DatadogStatsD.Events;
using DatadogStatsD.Metrics;
using DatadogStatsD.ServiceChecks;
using DatadogStatsD.Transport;

namespace DatadogStatsD
{
    /// <summary>
    /// DogStatsD client.
    /// </summary>
    public class DogStatsD : IDisposable
    {
        // https://github.com/statsd/statsd/blob/master/docs/metric_types.md#multi-metric-packets
        private const int UdpPayloadSize = 1432;
        private const int UdsPayloadSize = 8192;
        private const int MaxQueueSize = 1024;
        private const string UdpName = "udp";
        private const string UdsName = "uds";
        private static readonly TimeSpan MaxBufferingTime = TimeSpan.FromSeconds(2);
        private static readonly DogStatsDConfiguration DefaultConfiguration = new DogStatsDConfiguration();
        // https://docs.datadoghq.com/developers/dogstatsd/data_aggregation#how-is-aggregation-performed-with-the-dogstatsd-server
        private static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(10);
        private static readonly Timer TickTimer = new Timer(TickInterval.TotalMilliseconds) { Enabled = true };

        private readonly DogStatsDConfiguration _conf;
        private readonly byte[] _namespaceBytes;
        private readonly byte[] _sourceBytes;
        private readonly byte[] _constantTagsBytes;
        private readonly ITransport _transport;
        private readonly ITelemetry _telemetry;

        public DogStatsD() : this(DefaultConfiguration)
        {
        }

        public DogStatsD(DogStatsDConfiguration conf)
        {
            _conf = conf;
            _namespaceBytes = conf.Namespace != null ? DogStatsDSerializer.SerializeMetricName(conf.Namespace) : Array.Empty<byte>();
            _sourceBytes = DogStatsDSerializer.SerializeSource(_conf.Source);
            _constantTagsBytes = DogStatsDSerializer.ValidateAndSerializeTags(_conf.ConstantTags);

            ISocket socket;
            int maxBufferingSize;
            string transportName;
            if (conf.UnixSocketPath == null)
            {
                socket = new UdpSocket(_conf.Host ?? DefaultConfiguration.Host, _conf.Port);
                maxBufferingSize = UdpPayloadSize;
                transportName = UdpName;
            }
            else
            {
                socket = new UdsSocket(_conf.UnixSocketPath!);
                maxBufferingSize = UdsPayloadSize;
                transportName = UdsName;
            }

            _transport = new NonBlockingBufferedTransport(socket, maxBufferingSize, MaxBufferingTime, MaxQueueSize);
            _telemetry = _conf.Telemetry
                ? (ITelemetry)new Telemetry(transportName, _transport, TickTimer)
                : (ITelemetry)new NoopTelemetry();
            _transport.OnPacketSent += size => _telemetry.PacketSent(size);
            _transport.OnPacketDropped += (size, queue) => _telemetry.PacketDropped(size, queue);
        }

        public Count CreateCount(string metricName, IList<string>? tags = null)
        {
            return new Count(
                _transport,
                _telemetry,
                TickTimer,
                PrependNamespace(metricName),
                PrependConstantTags(tags));
        }

        public Histogram CreateHistogram(string metricName, double sampleRate = 1.0, IList<string>? tags = null)
        {
            return new Histogram(
                _transport,
                _telemetry,
                PrependNamespace(metricName),
                sampleRate,
                PrependConstantTags(tags));
        }

        public Gauge CreateGauge(string metricName, Func<double> evaluator, IList<string>? tags = null)
        {
            return new Gauge(
                _transport,
                _telemetry,
                TickTimer,
                PrependNamespace(metricName),
                evaluator,
                PrependConstantTags(tags));
        }

        public Distribution CreateDistribution(string metricName, double sampleRate = 1.0, IList<string>? tags = null)
        {
            return new Distribution(
                _transport,
                _telemetry,
                PrependNamespace(metricName),
                sampleRate,
                PrependConstantTags(tags));
        }

        public Set CreateSet(string metricName, IList<string>? tags = null)
        {
            return new Set(
                _transport,
                _telemetry,
                PrependNamespace(metricName),
                PrependConstantTags(tags));
        }

        /// <summary>
        /// Post an event to the stream. The source of the event is defined with <see cref="DogStatsDConfiguration.Source"/>.
        /// </summary>
        /// <param name="alertType">The level of alert of the event.</param>
        /// <param name="title">The event title. Limited to 100 characters.</param>
        /// <param name="message">The body of the event. Limited to 4000 characters. The text supports markdown.</param>
        /// <param name="priority">The priority of the event.</param>
        /// <param name="aggregationKey">
        /// An arbitrary string to use for aggregation. Limited to 100 characters. If you specify a key, all events
        /// using that key are grouped together in the Event Stream.
        /// </param>
        /// <param name="tags">A list of tags to apply to the event. They are appended to <see cref="DogStatsDConfiguration.ConstantTags"/>.</param>
        public void RaiseEvent(AlertType alertType, string title, string message, EventPriority priority = EventPriority.Normal,
            string? aggregationKey = null, IList<string>? tags = null)
        {
            _telemetry.EventSent();
            _transport.Send(DogStatsDSerializer.SerializeEvent(alertType, title, message, priority, _sourceBytes,
                aggregationKey, _constantTagsBytes, tags));
        }

        /// <summary>
        /// Send a service check.
        /// </summary>
        /// <param name="name">The service check name. <see cref="DogStatsDConfiguration.Namespace"/> is prepended to it.</param>
        /// <param name="checkStatus">The check status.</param>
        /// <param name="message">A message describing the current state of the service check.</param>
        /// <param name="tags">A list of tags to apply to the service check. They are appended to <see cref="DogStatsDConfiguration.ConstantTags"/>.</param>
        public void SendServiceCheck(string name, CheckStatus checkStatus, string message = "", IList<string>? tags = null)
        {
            _telemetry.ServiceCheckSent();
            _transport.Send(DogStatsDSerializer.SerializeServiceCheck(_namespaceBytes, name, checkStatus, message,
                _constantTagsBytes, tags));
        }

        public void Dispose()
        {
            _transport.Dispose();
            _telemetry.Dispose();
        }

        private string PrependNamespace(string metricName)
        {
            return string.IsNullOrEmpty(_conf.Namespace)
                ? metricName
                : _conf.Namespace + "." + metricName;
        }

        private IList<string>? PrependConstantTags(IList<string>? tags)
        {
            if (_conf.ConstantTags == null || _conf.ConstantTags.Count == 0)
            {
                return tags;
            }

            if (tags == null || tags.Count == 0)
            {
                return _conf.ConstantTags;
            }

            return _conf.ConstantTags.Concat(tags).ToList();
        }
    }
}