using System;
using System.Collections.Generic;
using DatadogStatsD.Metrics;
using NUnit.Framework;

namespace DatadogStatsD.Test
{
    internal class DogStatsDTest
    {
        private const string MetricName = "toto";
        private static readonly string[] Tags = { "a", "b" };

        [Test]
        public void ConstructorWithDefaultConfigurationShouldntThrow()
        {
            Assert.DoesNotThrow(() => new DogStatsD());
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
            Assert.Throws<ArgumentException>(() => CreateMetric(dog, type, name, 1.0, Tags));
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
            Assert.DoesNotThrow(() => CreateMetric(dog, type, name, 1.0, Tags));
        }

        [TestCase(MetricType.Histogram, 1.01)]
        [TestCase(MetricType.Histogram, -1.01)]
        [TestCase(MetricType.Distribution, 1.01)]
        [TestCase(MetricType.Distribution, -1.01)]
        public void InvalidSampleRateShouldThrow(MetricType type, double sampleRate)
        {
            var dog = new DogStatsD();
            Assert.Throws<ArgumentOutOfRangeException>(() => CreateMetric(dog, type, MetricName, sampleRate, Tags));
        }

        [TestCase(MetricType.Histogram, 1.00)]
        [TestCase(MetricType.Histogram, 0.99)]
        [TestCase(MetricType.Histogram, 0.5)]
        [TestCase(MetricType.Histogram, 0)]
        public void ValidSampleRateShouldNotThrow(MetricType type, double sampleRate)
        {
            var dog = new DogStatsD();
            Assert.DoesNotThrow(() => CreateMetric(dog, type, MetricName, sampleRate, Tags));
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
            Assert.Throws<ArgumentException>(() => CreateMetric(dog, type, MetricName, 1.0, new[] { tag }));
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
            Assert.DoesNotThrow(() => CreateMetric(dog, type, MetricName, 1.0, new[] { tag }));
        }

        private Metric CreateMetric(DogStatsD dog, MetricType type, string name, double sampleRate, IList<string>? tags)
        {
            return type switch
            {
                MetricType.Count => dog.CreateCount(name, tags),
                MetricType.Distribution => dog.CreateDistribution(name, sampleRate, tags),
                MetricType.Gauge => dog.CreateGauge(name, () => 0, tags),
                MetricType.Histogram => dog.CreateHistogram(name, sampleRate, tags),
                MetricType.Set => dog.CreateSet(name, tags),
                _ => throw new ArgumentException(nameof(type)),
            };
        }
    }
}