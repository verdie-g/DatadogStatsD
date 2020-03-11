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
        private static readonly TimeSpan MaxBufferingTime = TimeSpan.FromSeconds(2);
        private static readonly DogStatsDConfiguration DefaultConfiguration = new DogStatsDConfiguration();

        private readonly DogStatsDConfiguration _conf;
        private readonly ITransport _transport;

        public DogStatsD() : this(DefaultConfiguration)
        {
        }

        public DogStatsD(DogStatsDConfiguration conf)
        {
            _conf = conf;

            ISocket socket;
            int maxBufferingSize;
            if (conf.UnixSocketPath == null)
            {
                socket = new UdpSocket(
                    _conf.Host ?? DefaultConfiguration.Host,
                    _conf.Port ?? DefaultConfiguration.Port!.Value);
                maxBufferingSize = UdpPayloadSize;
            }
            else
            {
                socket = new UdsSocket(_conf.UnixSocketPath!);
                maxBufferingSize = UdsPayloadSize;
            }

            _transport = new NonBlockingBufferedTransport(socket, maxBufferingSize, MaxBufferingTime, MaxQueueSize);
        }

        public Count CreateCount(string metricName, double sampleRate = 1.0, IList<string>? tags = null)
        {
            return new Count(
                _transport,
                PrependNamespace(metricName),
                sampleRate,
                PrependConstantTags(tags));
        }

        public Histogram CreateHistogram(string metricName, double sampleRate = 1.0, IList<string>? tags = null)
        {
            return new Histogram(
                _transport,
                PrependNamespace(metricName),
                sampleRate,
                PrependConstantTags(tags));
        }

        public Timer CreateTimer(string metricName, double sampleRate = 1.0, IList<string>? tags = null)
        {
            return new Timer(
                _transport,
                PrependNamespace(metricName),
                sampleRate,
                PrependConstantTags(tags));
        }

        public void Dispose()
        {
            _transport.Dispose();
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