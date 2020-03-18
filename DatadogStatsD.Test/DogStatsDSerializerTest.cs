using System;
using System.Buffers;
using System.Text;
using DatadogStatsD.Events;
using DatadogStatsD.Metrics;
using NUnit.Framework;

namespace DatadogStatsD.Test
{
    internal class DogStatsDSerializerTest
    {
        [TestCase("foo", 1.0, MetricType.Count, null, null, "foo:1|c")]
        [TestCase("foo", 1.0, MetricType.Gauge, null, null, "foo:1|g")]
        [TestCase("foo", 1.0, MetricType.Distribution, null, null, "foo:1|d")]
        [TestCase("foo", 1.0, MetricType.Histogram, null, null, "foo:1|h")]
        [TestCase("foo", 1.0, MetricType.Set, null, null, "foo:1|s")]
        [TestCase("foo", -1.0, MetricType.Count, null, null, "foo:-1|c")]
        [TestCase("foo", 1.0, MetricType.Count, 1.0, null, "foo:1|c")]
        [TestCase("foo", 1.0, MetricType.Count, 0.100000, null, "foo:1|c|@0.100000")]
        [TestCase("foo", 1.0, MetricType.Count, 0.010000, null, "foo:1|c|@0.010000")]
        [TestCase("foo", 1.0, MetricType.Count, 0.001000, null, "foo:1|c|@0.001000")]
        [TestCase("foo", 1.0, MetricType.Count, 0.000100, null, "foo:1|c|@0.000100")]
        [TestCase("foo", 1.0, MetricType.Count, 0.000010, null, "foo:1|c|@0.000010")]
        [TestCase("foo", 1.0, MetricType.Count, 0.000001, null, "foo:1|c|@0.000001")]
        [TestCase("foo", 1.0, MetricType.Count, null, "a,b:c,d", "foo:1|c|#a,b:c,d")]
        [TestCase("foo", 0001.0, MetricType.Histogram, null, null, "foo:1|h")]
        [TestCase("foo", 0012.0, MetricType.Histogram, null, null, "foo:12|h")]
        [TestCase("foo", 0123.0, MetricType.Histogram, null, null, "foo:123|h")]
        [TestCase("foo", 1234.0, MetricType.Histogram, null, null, "foo:1234|h")]
        [TestCase("foo", 0.100000, MetricType.Histogram, null, null, "foo:0.100000|h")]
        [TestCase("foo", 0.120000, MetricType.Histogram, null, null, "foo:0.120000|h")]
        [TestCase("foo", 0.123000, MetricType.Histogram, null, null, "foo:0.123000|h")]
        [TestCase("foo", 0.123400, MetricType.Histogram, null, null, "foo:0.123400|h")]
        [TestCase("foo", 0.123450, MetricType.Histogram, null, null, "foo:0.123450|h")]
        [TestCase("foo", 0.123456, MetricType.Histogram, null, null, "foo:0.123456|h")]
        [TestCase("foo.bar_lol", -123456.789012, MetricType.Gauge, 0.123456, "a:b,cdef,fg:hij", "foo.bar_lol:-123456.789012|g|@0.123456|#a:b,cdef,fg:hij")]
        public void SerializeMetric(string name, double value, MetricType type, double? sampleRate, string tags,
            string expected)
        {
            byte[] nameBytes = DogStatsDSerializer.SerializeMetricName(name);
            byte[] typeBytes = DogStatsDSerializer.SerializeMetricType(type);
            byte[] sampleRateBytes = sampleRate != null ? DogStatsDSerializer.SerializeSampleRate(sampleRate.Value) : null;
            byte[] tagsBytes = DogStatsDSerializer.ValidateAndSerializeTags(tags?.Split(','));
            var metricBytes = DogStatsDSerializer.SerializeMetric(nameBytes, value, typeBytes, sampleRateBytes, tagsBytes);
            Assert.AreEqual(expected, Encoding.ASCII.GetString(metricBytes));
            ArrayPool<byte>.Shared.Return(metricBytes.Array);
        }

        [TestCase(AlertType.Info, "title", "message", Priority.Normal, null, null, null, null, "_e{005,0007}:title|message")]
        [TestCase(AlertType.Info, "C'est un événement", "", Priority.Normal, null, null, null, null, "_e{020,0006}:C'est un événement|")]
        [TestCase(AlertType.Success, "a", "b", Priority.Normal, null, null, null, null, "_e{001,0001}:a|b|t:success")]
        [TestCase(AlertType.Error, "a", "b", Priority.Normal, null, null, null, null, "_e{001,0001}:a|b|t:error")]
        [TestCase(AlertType.Warning, "a", "b", Priority.Normal, null, null, null, null, "_e{001,0001}:a|b|t:warning")]
        [TestCase(AlertType.Info, "a", "b", Priority.Low, null, null, null, null, "_e{001,0001}:a|b|p:low")]
        [TestCase(AlertType.Info, "a", "b", Priority.Normal, "cristaline", null, null, null, "_e{001,0001}:a|b|s:cristaline")]
        [TestCase(AlertType.Info, "a", "b", Priority.Normal, null, "aggr", null, null, "_e{001,0001}:a|b|k:aggr")]
        [TestCase(AlertType.Info, "a", "b", Priority.Normal, null, null, "ab,cd", null, "_e{001,0001}:a|b|#ab,cd")]
        [TestCase(AlertType.Info, "a", "b", Priority.Normal, null, null, null, "ef,gh", "_e{001,0001}:a|b|#ef,gh")]
        [TestCase(AlertType.Info, "a", "b", Priority.Normal, null, null, "ab,cd", "ef,gh", "_e{001,0001}:a|b|#ab,cd,ef,gh")]
        [TestCase(AlertType.Success, "Ton pote Jean-Mi", "contiguïté", Priority.Low, "evian", "clef", "ab,cd", "ef,gh", "_e{016,0012}:Ton pote Jean-Mi|contiguïté|p:low|t:success|k:clef|s:evian|#ab,cd,ef,gh")]
        public void SerializeEvent(AlertType alertType, string title, string message, Priority priority, string source,
            string aggregationKey, string constantTags, string extraTags, string expected)
        {
            byte[] sourceBytes = source != null ? Encoding.ASCII.GetBytes(source) : Array.Empty<byte>();
            byte[] constantTagsBytes = DogStatsDSerializer.ValidateAndSerializeTags(constantTags?.Split(','));
            string[] extraTagsList = extraTags?.Split(',');
            var eventBytes= DogStatsDSerializer.SerializeEvent(alertType, title, message, priority, sourceBytes, aggregationKey, constantTagsBytes, extraTagsList);
            string eventStr = Encoding.UTF8.GetString(eventBytes);
            Assert.AreEqual(expected, eventStr);
            ArrayPool<byte>.Shared.Return(eventBytes.Array);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa" +
                  "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
        [TestCase("韓國")]
        [TestCase("a#b")]
        [TestCase("a|b")]
        [TestCase("a@b")]
        public void SerializeMetricNameArgumentException(string name)
        {
            Assert.Throws<ArgumentException>(() => DogStatsDSerializer.SerializeMetricName(name));
        }

        [TestCase(10)]
        public void SerializeMetricTypeArgumentException(MetricType type)
        {
            Assert.Throws<System.IndexOutOfRangeException>(() => DogStatsDSerializer.SerializeMetricType(type));
        }

        [TestCase(0.0000001)]
        [TestCase(1.0000001)]
        [TestCase(10)]
        public void SerializeSampleRateArgumentException(double sampleRate)
        {
            Assert.Throws<ArgumentException>(() => DogStatsDSerializer.SerializeSampleRate(sampleRate));
        }
    }
}