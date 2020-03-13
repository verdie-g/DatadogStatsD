using System;
using System.Collections.Generic;
using System.Linq;
using DatadogStatsD.Metrics;
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

        private readonly DogStatsDConfiguration _conf;
        private readonly NonBlockingBufferedTransport _transport;
        private readonly Telemetry _telemetry;

        public DogStatsD() : this(DefaultConfiguration)
        {
        }

        public DogStatsD(DogStatsDConfiguration conf)
        {
            _conf = conf;

            ISocket socket;
            int maxBufferingSize;
            string transportName;
            if (conf.UnixSocketPath == null)
            {
                socket = new UdpSocket(
                    _conf.Host ?? DefaultConfiguration.Host,
                    _conf.Port ?? DefaultConfiguration.Port!.Value);
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
            _telemetry = new Telemetry(transportName, _transport);
            _transport.OnPacketSent += size => _telemetry.PacketSent(size);
            _transport.OnPacketDropped += (size, queue) => _telemetry.PacketDropped(size, queue);
        }

        public Count CreateCount(string metricName, double sampleRate = 1.0, IList<string>? tags = null)
        {
            return new Count(
                _transport,
                _telemetry,
                PrependNamespace(metricName),
                sampleRate,
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
                PrependNamespace(metricName),
                evaluator,
                PrependConstantTags(tags));
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