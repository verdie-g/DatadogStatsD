using System;
using System.Collections.Generic;
using DatadogStatsD.Metrics;
using DatadogStatsD.Transport;

namespace DatadogStatsD
{
    /// <summary>
    /// DogStatsD client.
    /// </summary>
    public class DogStatsD : IDisposable
    {
        private readonly DogStatsDConfiguration _conf;
        private readonly ITransport _transport;

        public DogStatsD(DogStatsDConfiguration conf)
        {
            _conf = conf;
            _transport = new UdpTransport(_conf.Host, _conf.Port);
        }

        public Count CreateCount(string metricName, double sampleRate = 1.0, IList<string> tags = null)
        {
            return new Count(
                _transport,
                PrependNamespace(metricName),
                sampleRate,
                PrependConstantTags(tags));
        }

        public Histogram CreateHistogram(string metricName, double sampleRate = 1.0, IList<string> tags = null)
        {
            return new Histogram(
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

        private IList<string> PrependConstantTags(IList<string> tags)
        {
            if (_conf.ConstantTags == null || _conf.ConstantTags.Count == 0)
            {
                return tags;
            }

            if (tags == null || tags.Count == 0)
            {
                return _conf.ConstantTags;
            }

            var allTags = new List<string>(_conf.ConstantTags);
            allTags.AddRange(tags);
            return allTags;
        }
    }
}