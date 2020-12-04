using System;
using System.Collections.Generic;
using DatadogStatsD.Events;
using DatadogStatsD.Metrics;
using DatadogStatsD.ServiceChecks;
using NUnit.Framework;

namespace DatadogStatsD.Test
{
    internal class DogStatsDTest
    {
        private const string MetricName = "toto";
        private static readonly KeyValuePair<string, string>[] Tags =
        {
            KeyValuePair.Create("a", ""),
            KeyValuePair.Create("b", ""),
        };

        [Test]
        public void ConstructorWithDefaultConfigurationShouldntThrow()
        {
            Assert.DoesNotThrow(() => new DogStatsD());
        }

        [Test]
        public void ConstructorWithNullEndPointShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(() => new DogStatsD(new DogStatsDConfiguration { EndPoint = null! }));
        }

        [TestCase(MetricType.Count, "")]
        [TestCase(MetricType.Count, "1a")]
        [TestCase(MetricType.Count, "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
        [TestCase(MetricType.Count, "a!b")]
        [TestCase(MetricType.Count, "a#b")]
        [TestCase(MetricType.Count, "a-b")]
        [TestCase(MetricType.Count, "a*b")]
        [TestCase(MetricType.Distribution, "")]
        [TestCase(MetricType.Gauge, "")]
        [TestCase(MetricType.Histogram, "")]
        [TestCase(MetricType.Set, "")]
        public void InvalidMetricNameShouldThrow(MetricType type, string name)
        {
            var dog = new DogStatsD();
            Assert.Throws<ArgumentException>(() => CreateMetric(dog, type, name, Tags));
        }

        [TestCase(MetricType.Count)]
        [TestCase(MetricType.Distribution)]
        [TestCase(MetricType.Gauge)]
        [TestCase(MetricType.Histogram)]
        [TestCase(MetricType.Set)]
        public void NullMetricNameShouldThrow(MetricType type)
        {
            var dog = new DogStatsD();
            Assert.Throws<ArgumentNullException>(() => CreateMetric(dog, type, null!, Tags));
        }

        [TestCase(MetricType.Count, "a")]
        [TestCase(MetricType.Count, "abc_def")]
        [TestCase(MetricType.Count, "abc.def")]
        [TestCase(MetricType.Count, "abc.def_ghi")]
        [TestCase(MetricType.Count, "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
        [TestCase(MetricType.Count, "abc.def_123")]
        [TestCase(MetricType.Distribution, "abc.def_123")]
        [TestCase(MetricType.Gauge, "abc.def_123")]
        [TestCase(MetricType.Histogram, "abc.def_123")]
        [TestCase(MetricType.Set, "abc.def_123")]
        public void ValueMetricNameShouldNotThrow(MetricType type, string name)
        {
            var dog = new DogStatsD();
            Assert.DoesNotThrow(() => CreateMetric(dog, type, name, Tags));
        }

        [TestCase(MetricType.Count, "1a")]
        [TestCase(MetricType.Count, "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
        [TestCase(MetricType.Count, "a!b")]
        [TestCase(MetricType.Count, "a#b")]
        [TestCase(MetricType.Count, "a*b")]
        [TestCase(MetricType.Count, "a\\b")]
        public void InvalidTagsShouldThrow(MetricType type, string tag)
        {
            var dog = new DogStatsD();
            Assert.Throws<ArgumentException>(() => CreateMetric(dog, type, MetricName, TestHelper.ParseTags(tag)));
        }

        [TestCase(MetricType.Count, "a1")]
        [TestCase(MetricType.Count, "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
        [TestCase(MetricType.Count, "a_b")]
        [TestCase(MetricType.Count, "a-b")]
        [TestCase(MetricType.Count, "a:b")]
        [TestCase(MetricType.Count, "a.b")]
        [TestCase(MetricType.Count, "a/b")]
        [TestCase(MetricType.Count, "a_b-c:d.e/f")]
        public void ValidTagsShouldNotThrow(MetricType type, string tag)
        {
            var dog = new DogStatsD();
            Assert.DoesNotThrow(() => CreateMetric(dog, type, MetricName, TestHelper.ParseTags(tag)));
        }

        [Test]
        public void RaiseEventNullTitleShouldThrow()
        {
            var dog = new DogStatsD();
            Assert.Throws<ArgumentNullException>(() => dog.RaiseEvent(AlertType.Info, null!, ""));
        }

        [Test]
        public void RaiseEventNullMessageShouldThrow()
        {
            var dog = new DogStatsD();
            Assert.Throws<ArgumentNullException>(() => dog.RaiseEvent(AlertType.Info, "", null!));
        }

        [Test]
        public void SendServiceCheckNullNameShouldThrow()
        {
            var dog = new DogStatsD();
            Assert.Throws<ArgumentNullException>(() => dog.SendServiceCheck(null!, CheckStatus.Ok));
        }

        [Test]
        public void SendServiceCheckNullMessageShouldThrow()
        {
            var dog = new DogStatsD();
            Assert.Throws<ArgumentNullException>(() => dog.SendServiceCheck("", CheckStatus.Ok, null!));
        }

        private Metric CreateMetric(DogStatsD dog, MetricType type, string name, IList<KeyValuePair<string, string>>? tags)
        {
            return type switch
            {
                MetricType.Count => dog.CreateCount(name, tags),
                MetricType.Distribution => dog.CreateDistribution(name, tags),
                MetricType.Gauge => dog.CreateGauge(name, () => 0, tags),
                MetricType.Histogram => dog.CreateHistogram(name, tags),
                MetricType.Set => dog.CreateSet(name, tags),
                _ => throw new ArgumentException(nameof(type)),
            };
        }
    }
}
