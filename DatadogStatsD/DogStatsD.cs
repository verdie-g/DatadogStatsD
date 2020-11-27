using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using DatadogStatsD.Events;
using DatadogStatsD.Metrics;
using DatadogStatsD.Protocol;
using DatadogStatsD.ServiceChecks;
using DatadogStatsD.Telemetering;
using DatadogStatsD.Ticking;
using DatadogStatsD.Transport;

namespace DatadogStatsD
{
    /// <summary>
    /// DogStatsD client.
    /// </summary>
    public class DogStatsD
#if NETSTANDARD2_0
        : IDisposable
#else
        : IAsyncDisposable
#endif
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
        private static readonly ITimer TickTimer = new TimerWrapper(TickInterval);
        private static readonly byte[] SourceBytes = DogStatsDSerializer.SerializeSource("csharp");

        private readonly string _namespace;
        private readonly byte[] _namespaceBytes;
        private readonly KeyValuePair<string, string>[] _constantTags;
        private readonly byte[] _constantTagsBytes;
        private readonly ITransport _transport;
        private readonly ITelemetry _telemetry;

        /// <summary>
        /// Instantiates a new DogstatsD client using the default configuration, that is, UDP on localhost:8125.
        /// </summary>
        public DogStatsD() : this(DefaultConfiguration)
        {
        }

        /// <summary>
        /// Instantiates a new DogstatsD client using the specified configuration.
        /// </summary>
        public DogStatsD(DogStatsDConfiguration conf)
        {
            conf = conf ?? throw new ArgumentNullException(nameof(conf));
            conf.EndPoint = conf.EndPoint ?? throw new ArgumentNullException(nameof(conf.EndPoint));
            _namespace = conf.Namespace ?? string.Empty;
            _namespaceBytes = conf.Namespace != null ? DogStatsDSerializer.SerializeMetricName(conf.Namespace) : Array.Empty<byte>();
            _constantTags = MergeTags(GetConstantTagsFromEnvironment(), conf.ConstantTags).ToArray();
            _constantTagsBytes = DogStatsDSerializer.ValidateAndSerializeTags(_constantTags);

            int maxBufferingSize;
            string transportName;
            if (conf.EndPoint.AddressFamily == AddressFamily.Unix)
            {
                maxBufferingSize = UdsPayloadSize;
                transportName = UdsName;
            }
            else
            {
                maxBufferingSize = UdpPayloadSize;
                transportName = UdpName;
            }

            var socket = new SocketWrapper(conf.EndPoint);
            _transport = new NonBlockingBufferedTransport(socket, maxBufferingSize, MaxBufferingTime, MaxQueueSize);
            _telemetry = conf.Telemetry
                ? (ITelemetry)new Telemetry(transportName, _transport, TickTimer, _constantTags)
                : (ITelemetry)new NoopTelemetry();
            _transport.OnPacketSent += size => _telemetry.PacketSent(size);
            _transport.OnPacketDropped += (size, queue) => _telemetry.PacketDropped(size, queue);
        }

        /// <summary>
        /// Creates a new <see cref="Count"/> bound to the current <see cref="DogStatsD"/> instance.
        /// </summary>
        /// <param name="metricName">Name of the metric.</param>
        /// <param name="tags">Tags to add to the metric in addition to <see cref="DogStatsDConfiguration.ConstantTags"/>.</param>
        public Count CreateCount(string metricName, IList<KeyValuePair<string, string>>? tags = null)
        {
            return new Count(
                _transport,
                _telemetry,
                TickTimer,
                PrependNamespace(metricName),
                PrependConstantTags(tags));
        }

        /// <summary>
        /// Creates a new <see cref="Histogram"/> bound to the current <see cref="DogStatsD"/> instance.
        /// </summary>
        /// <param name="metricName">Name of the metric.</param>
        /// <param name="sampleRate">Sample rate to apply to the metric. Takes a value between 0 (everything is sampled, so nothing is sent) and 1 (no sample).</param>
        /// <param name="tags">Tags to add to the metric in addition to <see cref="DogStatsDConfiguration.ConstantTags"/>.</param>
        public Histogram CreateHistogram(string metricName, double sampleRate = 1.0, IList<KeyValuePair<string, string>>? tags = null)
        {
            return new Histogram(
                _transport,
                _telemetry,
                PrependNamespace(metricName),
                sampleRate,
                PrependConstantTags(tags));
        }

        /// <summary>
        /// Creates a new <see cref="Gauge"/> bound to the current <see cref="DogStatsD"/> instance.
        /// </summary>
        /// <param name="metricName">Name of the metric.</param>
        /// <param name="evaluator">
        /// An optional function that will be periodically evaluated to get the value associated with the metric.
        /// The function should be fast and should not throw (else nothing is sent). If the value was recently updated
        /// with <see cref="Gauge.Update"/>, it will be used over what <paramref name="evaluator"/> returns.
        /// </param>
        /// <param name="tags">Tags to add to the metric in addition to <see cref="DogStatsDConfiguration.ConstantTags"/>.</param>
        public Gauge CreateGauge(string metricName, Func<double>? evaluator = null, IList<KeyValuePair<string, string>>? tags = null)
        {
            return new Gauge(
                _transport,
                _telemetry,
                TickTimer,
                PrependNamespace(metricName),
                evaluator,
                PrependConstantTags(tags));
        }

        /// <summary>
        /// Creates a new <see cref="Distribution"/> bound to the current <see cref="DogStatsD"/> instance.
        /// </summary>
        /// <param name="metricName">Name of the metric.</param>
        /// <param name="sampleRate">Sample rate to apply to the metric. Takes a value between 0 (everything is sampled, so nothing is sent) and 1 (no sample).</param>
        /// <param name="tags">Tags to add to the metric in addition to <see cref="DogStatsDConfiguration.ConstantTags"/>.</param>
        public Distribution CreateDistribution(string metricName, double sampleRate = 1.0, IList<KeyValuePair<string, string>>? tags = null)
        {
            return new Distribution(
                _transport,
                _telemetry,
                PrependNamespace(metricName),
                sampleRate,
                PrependConstantTags(tags));
        }

        /// <summary>
        /// Creates a new <see cref="Set"/> bound to the current <see cref="DogStatsD"/> instance.
        /// </summary>
        /// <param name="metricName">Name of the metric.</param>
        /// <param name="tags">Tags to add to the metric in addition to <see cref="DogStatsDConfiguration.ConstantTags"/>.</param>
        public Set CreateSet(string metricName, IList<KeyValuePair<string, string>>? tags = null)
        {
            return new Set(
                _transport,
                _telemetry,
                TickTimer,
                PrependNamespace(metricName),
                PrependConstantTags(tags));
        }

        /// <summary>
        /// Post an event to the stream.
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
            string? aggregationKey = null, IList<KeyValuePair<string, string>>? tags = null)
        {
            title = title ?? throw new ArgumentNullException(nameof(title));
            message = message ?? throw new ArgumentNullException(nameof(message));

            _telemetry.EventSent();
            _transport.Send(DogStatsDSerializer.SerializeEvent(alertType, title, message, priority, SourceBytes,
                aggregationKey, _constantTagsBytes, tags));
        }

        /// <summary>
        /// Send a service check.
        /// </summary>
        /// <param name="name">The service check name. <see cref="DogStatsDConfiguration.Namespace"/> is prepended to it.</param>
        /// <param name="checkStatus">The check status.</param>
        /// <param name="message">A message describing the current state of the service check.</param>
        /// <param name="tags">A list of tags to apply to the service check. They are appended to <see cref="DogStatsDConfiguration.ConstantTags"/>.</param>
        public void SendServiceCheck(string name, CheckStatus checkStatus, string message = "", IList<KeyValuePair<string, string>>? tags = null)
        {
            name = name ?? throw new ArgumentNullException(nameof(name));
            message = message ?? throw new ArgumentNullException(nameof(message));

            _telemetry.ServiceCheckSent();
            _transport.Send(DogStatsDSerializer.SerializeServiceCheck(_namespaceBytes, name, checkStatus, message,
                _constantTagsBytes, tags));
        }

        /// <summary>
        /// Flushes the buffered messages and releases all resources used by the current <see cref="DogStatsD"/> instance.
        /// </summary>
#if NETSTANDARD2_0
        public void Dispose()
        {
            _telemetry.Dispose();
            _transport.Dispose();
        }
#else
        public ValueTask DisposeAsync()
        {
            _telemetry.Dispose();
            return _transport.DisposeAsync();
        }
#endif

        private string PrependNamespace(string metricName)
        {
            // Use this method, which is used in all methods to create metrics, to check if the user didn't respect the contract
            metricName = metricName ?? throw new ArgumentNullException(nameof(metricName));

            return _namespace.Length == 0
                ? metricName
                : _namespace + "." + metricName;
        }

        private IList<KeyValuePair<string, string>>? PrependConstantTags(IList<KeyValuePair<string, string>>? tags)
        {
            if (_constantTags.Length == 0)
            {
                return tags;
            }

            if (tags == null || tags.Count == 0)
            {
                return _constantTags;
            }

            return MergeTags(_constantTags, tags);
        }

        /// <summary>
        /// Merges two sets of tags. If a set has several tags with the same key, the last one is kept. If the two sets
        /// have tags with the same key, the tags from <paramref name="tags2"/> are kept.
        /// </summary>
        private static IList<KeyValuePair<string, string>> MergeTags(IEnumerable<KeyValuePair<string, string>>? tags1,
            IEnumerable<KeyValuePair<string, string>>? tags2)
        {
            var tags = new Dictionary<string, string>(StringComparer.Ordinal);
            if (tags1 != null)
            {
                foreach (var tag in tags1)
                {
                    tags[tag.Key] = tag.Value;
                }
            }

            if (tags2 != null)
            {
                foreach (var tag in tags2)
                {
                    tags[tag.Key] = tag.Value;
                }
            }

            return tags.ToArray();
        }

        private static IEnumerable<KeyValuePair<string, string>> GetConstantTagsFromEnvironment()
        {
            KeyValuePair<string, string>[] supportedVariables =
            {
                new KeyValuePair<string, string>("DD_ENV", "env"),
                new KeyValuePair<string, string>("DD_SERVICE", "service"),
                new KeyValuePair<string, string>("DD_VERSION", "version"),
            };

            foreach (var variable in supportedVariables)
            {
                string? value = Environment.GetEnvironmentVariable(variable.Key);
                if (!string.IsNullOrEmpty(value))
                {
                    yield return new KeyValuePair<string, string>(variable.Value, value);
                }
            }
        }
    }
}
