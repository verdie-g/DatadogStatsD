using System;
using System.Collections.Generic;
using DatadogStatsD.Metrics;
using DatadogStatsD.Telemetering;
using DatadogStatsD.Transport;
using Moq;
using NUnit.Framework;

namespace DatadogStatsD.Test.Metrics
{
    public class HistogramTest
    {
        private const string MetricName = "toto";
        private static readonly IList<KeyValuePair<string, string>> Tags = new[]
        {
            KeyValuePair.Create("abc", "def"),
            KeyValuePair.Create("ghi", ""),
        };

        [Test]
        public void RecordShouldThrowIfValueIsNaN()
        {
            var transport = new Mock<ITransport>();
            var telemetry = new Mock<ITelemetry>();
            var h = new Histogram(transport.Object, telemetry.Object, MetricName, 1.0, Tags);

            Assert.Throws<ArgumentException>(() => h.Sample(double.NaN));
        }

        [Test]
        public void RecordShouldSentBytesToTransport()
        {
            var transport = new Mock<ITransport>();
            var telemetry = new Mock<ITelemetry>();
            var h = new Histogram(transport.Object, telemetry.Object, MetricName, 1.0, Tags);

            h.Sample(123.456);
            transport.Verify(t => t.Send(It.IsAny<ArraySegment<byte>>()), Times.Exactly(1));
            telemetry.Verify(t => t.MetricSent(), Times.Exactly(1));

            h.Sample(789);
            transport.Verify(t => t.Send(It.IsAny<ArraySegment<byte>>()), Times.Exactly(2));
            telemetry.Verify(t => t.MetricSent(), Times.Exactly(2));
        }
    }
}
