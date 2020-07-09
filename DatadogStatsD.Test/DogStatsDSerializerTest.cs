using System;
using System.Buffers;
using System.Text;
using DatadogStatsD.Events;
using DatadogStatsD.Metrics;
using DatadogStatsD.Protocol;
using DatadogStatsD.ServiceChecks;
using NUnit.Framework;

namespace DatadogStatsD.Test
{
    internal class DogStatsDSerializerTest
    {
        [TestCase("foo", 1.0, MetricType.Count, 1.0, null, "foo:1|c")]
        [TestCase("foo", 1.0, MetricType.Gauge, 1.0, null, "foo:1|g")]
        [TestCase("foo", 1.0, MetricType.Distribution, 1.0, null, "foo:1|d")]
        [TestCase("foo", 1.0, MetricType.Histogram, 1.0, null, "foo:1|h")]
        [TestCase("foo", 1.0, MetricType.Set, 1.0, null, "foo:1|s")]
        [TestCase("foo", -1.0, MetricType.Count, 1.0, null, "foo:-1|c")]
        [TestCase("foo", 9007199254740992, MetricType.Count, 1.0, null, "foo:9007199254740992|c")]
        [TestCase("foo", 1.0, MetricType.Count, 1.0, null, "foo:1|c")]
        [TestCase("foo", 1.0, MetricType.Count, 0.1, null, "foo:1|c|@0.1")]
        [TestCase("foo", 1.0, MetricType.Count, 0.01, null, "foo:1|c|@0.01")]
        [TestCase("foo", 1.0, MetricType.Count, 0.001, null, "foo:1|c|@0.001")]
        [TestCase("foo", 1.0, MetricType.Count, 0.0001, null, "foo:1|c|@0.0001")]
        [TestCase("foo", 1.0, MetricType.Count, 0.00001, null, "foo:1|c|@1E-05")]
        [TestCase("foo", 1.0, MetricType.Count, 0.000001, null, "foo:1|c|@1E-06")]
        [TestCase("foo", 1.0, MetricType.Count, 1.0, "a,b:c,d", "foo:1|c|#a,b:c,d")]
        [TestCase("foo", 1.0, MetricType.Histogram, 1.0, null, "foo:1|h")]
        [TestCase("foo", 12.0, MetricType.Histogram, 1.0, null, "foo:12|h")]
        [TestCase("foo", 123.0, MetricType.Histogram, 1.0, null, "foo:123|h")]
        [TestCase("foo", 1234.0, MetricType.Histogram, 1.0, null, "foo:1234|h")]
        [TestCase("foo", 0.1, MetricType.Histogram, 1.0, null, "foo:0.1|h")]
        [TestCase("foo", 0.12, MetricType.Histogram, 1.0, null, "foo:0.12|h")]
        [TestCase("foo", 0.123, MetricType.Histogram, 1.0, null, "foo:0.123|h")]
        [TestCase("foo", 0.1234, MetricType.Histogram, 1.0, null, "foo:0.1234|h")]
        [TestCase("foo", 0.12345, MetricType.Histogram, 1.0, null, "foo:0.12345|h")]
        [TestCase("foo", 0.123456, MetricType.Histogram, 1.0, null, "foo:0.123456|h")]
        [TestCase("foo", -1.123456789E-7, MetricType.Histogram, 1.0, null, "foo:-1.123456789E-07|h")]
        [TestCase("foo.bar_lol", -123456.789012, MetricType.Gauge, 0.123456, "a:b,cdef,fg:hij", "foo.bar_lol:-123456.789012|g|@0.123456|#a:b,cdef,fg:hij")]
        public void SerializeMetric(string name, double value, MetricType type, double sampleRate, string tags,
            string expected)
        {
            byte[] nameBytes = DogStatsDSerializer.SerializeMetricName(name);
            byte[] sampleRateBytes = DogStatsDSerializer.SerializeSampleRate(sampleRate);
            byte[] tagsBytes = DogStatsDSerializer.ValidateAndSerializeTags(tags?.Split(','));
            var metricBytes = DogStatsDSerializer.SerializeMetric(nameBytes, value, type, sampleRateBytes, tagsBytes);
            Assert.AreEqual(expected, Encoding.ASCII.GetString(metricBytes));
            ArrayPool<byte>.Shared.Return(metricBytes.Array!);
        }

        [TestCase(AlertType.Info, "title", "message", EventPriority.Normal, null, null, null, null, "_e{005,0007}:title|message")]
        [TestCase(AlertType.Info, "abc\ndef\r\nghi", "abc\ndef\r\nghi", EventPriority.Normal, null, null, null, null, "_e{014,0014}:abc\\ndef\r\\nghi|abc\\ndef\r\\nghi")]
        [TestCase(AlertType.Info, "C'est un événement", "", EventPriority.Normal, null, null, null, null, "_e{020,0006}:C'est un événement|")]
        [TestCase(AlertType.Success, "a", "b", EventPriority.Normal, null, null, null, null, "_e{001,0001}:a|b|t:success")]
        [TestCase(AlertType.Error, "a", "b", EventPriority.Normal, null, null, null, null, "_e{001,0001}:a|b|t:error")]
        [TestCase(AlertType.Warning, "a", "b", EventPriority.Normal, null, null, null, null, "_e{001,0001}:a|b|t:warning")]
        [TestCase(AlertType.Info, "a", "b", EventPriority.Low, null, null, null, null, "_e{001,0001}:a|b|p:low")]
        [TestCase(AlertType.Info, "a", "b", EventPriority.Normal, "cristaline", null, null, null, "_e{001,0001}:a|b|s:cristaline")]
        [TestCase(AlertType.Info, "a", "b", EventPriority.Normal, null, "aggr", null, null, "_e{001,0001}:a|b|k:aggr")]
        [TestCase(AlertType.Info, "a", "b", EventPriority.Normal, null, null, "ab,cd", null, "_e{001,0001}:a|b|#ab,cd")]
        [TestCase(AlertType.Info, "a", "b", EventPriority.Normal, null, null, null, "ef,gh", "_e{001,0001}:a|b|#ef,gh")]
        [TestCase(AlertType.Info, "a", "b", EventPriority.Normal, null, null, "ab,cd", "ef,gh", "_e{001,0001}:a|b|#ab,cd,ef,gh")]
        [TestCase(AlertType.Success, "Ton pote Jean-Mi", "contiguïté", EventPriority.Low, "evian", "clef", "ab,cd", "ef,gh", "_e{016,0012}:Ton pote Jean-Mi|contiguïté|p:low|t:success|k:clef|s:evian|#ab,cd,ef,gh")]
        public void SerializeEvent(AlertType alertType, string title, string message, EventPriority priority, string? source,
            string? aggregationKey, string? constantTags, string? extraTags, string expected)
        {
            byte[] sourceBytes = source != null ? Encoding.ASCII.GetBytes(source) : Array.Empty<byte>();
            byte[] constantTagsBytes = DogStatsDSerializer.ValidateAndSerializeTags(constantTags?.Split(','));
            string[]? extraTagsList = extraTags?.Split(',');
            var eventBytes= DogStatsDSerializer.SerializeEvent(alertType, title, message, priority, sourceBytes, aggregationKey, constantTagsBytes, extraTagsList);
            string eventStr = Encoding.UTF8.GetString(eventBytes);
            Assert.AreEqual(expected, eventStr);
            ArrayPool<byte>.Shared.Return(eventBytes.Array!);
        }

        [TestCase(null, "cd", CheckStatus.Ok, null, null, null, "_sc|cd|0")]
        [TestCase("ab", "cd", CheckStatus.Ok, null, null, null, "_sc|ab.cd|0")]
        [TestCase(null, "cd", CheckStatus.Warning, null, null, null, "_sc|cd|1")]
        [TestCase(null, "cd", CheckStatus.Critical, null, null, null, "_sc|cd|2")]
        [TestCase(null, "cd", CheckStatus.Unknown, null, null, null, "_sc|cd|3")]
        [TestCase(null, "cd", CheckStatus.Ok, "é ä ù", null, null, "_sc|cd|0|m:é ä ù")]
        [TestCase(null, "cd", CheckStatus.Ok, "abc\nm: def\r\nghi", null, null, "_sc|cd|0|m:abc\\nm\\: def\r\\nghi")]
        [TestCase(null, "cd", CheckStatus.Ok, null, "ef,gh", null, "_sc|cd|0|#ef,gh")]
        [TestCase(null, "cd", CheckStatus.Ok, null, null, "ij,kl", "_sc|cd|0|#ij,kl")]
        [TestCase("ab", "cd", CheckStatus.Ok, "aaa", "ef,gh", "ij,kl", "_sc|ab.cd|0|#ef,gh,ij,kl|m:aaa")]
        public void SerializeServiceCheck(string ns, string name, CheckStatus checkStatus, string message, string constantTags,
            string extraTags, string expected)
        {
            byte[] nsBytes = ns != null ? Encoding.ASCII.GetBytes(ns) : Array.Empty<byte>();
            message ??= string.Empty;
            byte[] constantTagsBytes = DogStatsDSerializer.ValidateAndSerializeTags(constantTags?.Split(','));
            string[]? extraTagsList = extraTags?.Split(',');
            var serviceCheckBytes = DogStatsDSerializer.SerializeServiceCheck(nsBytes, name, checkStatus, message, constantTagsBytes, extraTagsList);
            string serviceCheckStr = Encoding.UTF8.GetString(serviceCheckBytes);
            Assert.AreEqual(expected, serviceCheckStr);
            ArrayPool<byte>.Shared.Return(serviceCheckBytes.Array!);
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

        [TestCase(-0.5)]
        [TestCase(1.5)]
        public void SerializeSampleRateArgumentException(double sampleRate)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => DogStatsDSerializer.SerializeSampleRate(sampleRate));
        }

        [Test]
        public void EventTitleIsTruncated()
        {
            string title = new string('a', 101);
            var serviceCheckBytes = DogStatsDSerializer.SerializeEvent(AlertType.Info, title, "", EventPriority.Normal,
                Array.Empty<byte>(), null, Array.Empty<byte>(), null);
            string serviceCheckStr = Encoding.UTF8.GetString(serviceCheckBytes);
            Assert.AreEqual($"_e{{100,0000}}:{title.Substring(0, 100)}|", serviceCheckStr);
        }

        [Test]
        public void EventMessageIsTruncated()
        {
            string message = new string('a', 4001);
            var serviceCheckBytes = DogStatsDSerializer.SerializeEvent(AlertType.Info, "", message, EventPriority.Normal,
                Array.Empty<byte>(), null, Array.Empty<byte>(), null);
            string serviceCheckStr = Encoding.UTF8.GetString(serviceCheckBytes);
            Assert.AreEqual($"_e{{000,4000}}:|{message.Substring(0, 4000)}", serviceCheckStr);
        }

        [Test]
        public void EventAggregationKeyIsTruncated()
        {
            string aggregationKey = new string('a', 101);
            var serviceCheckBytes = DogStatsDSerializer.SerializeEvent(AlertType.Info, "", "", EventPriority.Normal,
                Array.Empty<byte>(), aggregationKey, Array.Empty<byte>(), null);
            string serviceCheckStr = Encoding.UTF8.GetString(serviceCheckBytes);
            Assert.AreEqual($"_e{{000,0000}}:||k:{aggregationKey.Substring(0, 100)}", serviceCheckStr);
        }
    }
}