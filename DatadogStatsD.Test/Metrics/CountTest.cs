using System;
using System.Collections.Generic;
using DatadogStatsD.Metrics;
using DatadogStatsD.Telemetering;
using DatadogStatsD.Transport;
using Moq;
using NUnit.Framework;

namespace DatadogStatsD.Test.Metrics
{
    public class CountTest
    {
        private const string MetricName = "toto";
        private static readonly IList<KeyValuePair<string, string>> Tags = new[]
        {
            KeyValuePair.Create("abc", "def"),
            KeyValuePair.Create("ghi", ""),
        };

        [Test]
        public void CountShouldBeFlushedEveryTick()
        {
            var transport = new Mock<ITransport>();
            var telemetry = new Mock<ITelemetry>();
            var timer = new ManualTimer();
            var c = new Count(transport.Object, telemetry.Object, timer, MetricName, Tags);

            c.Increment();
            timer.TriggerElapsed();
            transport.Verify(t => t.Send(It.IsAny<ArraySegment<byte>>()), Times.Exactly(1));
            telemetry.Verify(t => t.MetricSent(), Times.Exactly(1));

            c.Increment(5);
            timer.TriggerElapsed();
            transport.Verify(t => t.Send(It.IsAny<ArraySegment<byte>>()), Times.Exactly(2));
            telemetry.Verify(t => t.MetricSent(), Times.Exactly(2));
        }

        [Test]
        public void CountShouldntBeFlushedIfDidntChange()
        {
            var transport = new Mock<ITransport>();
            var telemetry = new Mock<ITelemetry>();
            var timer = new ManualTimer();
            var c = new Count(transport.Object, telemetry.Object, timer, MetricName, Tags);

            timer.TriggerElapsed();
            transport.Verify(t => t.Send(It.IsAny<ArraySegment<byte>>()), Times.Exactly(0));
            telemetry.Verify(t => t.MetricSent(), Times.Exactly(0));

            timer.TriggerElapsed();
            transport.Verify(t => t.Send(It.IsAny<ArraySegment<byte>>()), Times.Exactly(0));
            telemetry.Verify(t => t.MetricSent(), Times.Exactly(0));
        }

        [Test]
        public void CountShouldntBeFlushedIfDecrementsPlusIncrementsEqualZero()
        {
            var transport = new Mock<ITransport>();
            var telemetry = new Mock<ITelemetry>();
            var timer = new ManualTimer();
            var c = new Count(transport.Object, telemetry.Object, timer, MetricName, Tags);

            c.Increment();
            c.Increment(4);
            c.Decrement(5);
            timer.TriggerElapsed();
            transport.Verify(t => t.Send(It.IsAny<ArraySegment<byte>>()), Times.Exactly(0));
            telemetry.Verify(t => t.MetricSent(), Times.Exactly(0));
        }


        [Test]
        public void CountShouldFlushOnDispose()
        {
            var transport = new Mock<ITransport>();
            var telemetry = new Mock<ITelemetry>();
            var timer = new ManualTimer();
            var c = new Count(transport.Object, telemetry.Object, timer, MetricName, Tags);

            c.Increment();
            c.Dispose();
            transport.Verify(t => t.Send(It.IsAny<ArraySegment<byte>>()), Times.Exactly(1));
            telemetry.Verify(t => t.MetricSent(), Times.Exactly(1));
        }
    }
}
