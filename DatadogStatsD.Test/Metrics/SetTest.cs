using System;
using DatadogStatsD.Metrics;
using DatadogStatsD.Telemetering;
using DatadogStatsD.Transport;
using Moq;
using NUnit.Framework;

namespace DatadogStatsD.Test.Metrics
{
    public class SetTest
    {
        private const string MetricName = "toto";

        [Test]
        public void SetShouldBeFlushedEveryTick()
        {
            var transport = new Mock<ITransport>();
            var telemetry = new Mock<ITelemetry>();
            var timer = new ManualTimer();
            var s = new Set(transport.Object, telemetry.Object, timer, MetricName, null);

            s.Add(5);
            timer.TriggerElapsed();
            transport.Verify(t => t.Send(It.IsAny<ArraySegment<byte>>()), Times.Exactly(1));
            telemetry.Verify(t => t.MetricSent(), Times.Exactly(1));

            s.Add(6);
            timer.TriggerElapsed();
            transport.Verify(t => t.Send(It.IsAny<ArraySegment<byte>>()), Times.Exactly(2));
            telemetry.Verify(t => t.MetricSent(), Times.Exactly(2));
        }

        [Test]
        public void SetShouldntBeFlushedIfDidntChange()
        {
            var transport = new Mock<ITransport>();
            var telemetry = new Mock<ITelemetry>();
            var timer = new ManualTimer();
            var s = new Set(transport.Object, telemetry.Object, timer, MetricName, null);

            timer.TriggerElapsed();
            transport.Verify(t => t.Send(It.IsAny<ArraySegment<byte>>()), Times.Exactly(0));
            telemetry.Verify(t => t.MetricSent(), Times.Exactly(0));

            timer.TriggerElapsed();
            transport.Verify(t => t.Send(It.IsAny<ArraySegment<byte>>()), Times.Exactly(0));
            telemetry.Verify(t => t.MetricSent(), Times.Exactly(0));
        }

        [Test]
        public void SetShouldSendOneMetricByUniqueValue()
        {
            var transport = new Mock<ITransport>();
            var telemetry = new Mock<ITelemetry>();
            var timer = new ManualTimer();
            var s = new Set(transport.Object, telemetry.Object, timer, MetricName, null);

            s.Add(5);
            s.Add(6);
            s.Add(7);
            s.Add(5);
            s.Add(7);
            timer.TriggerElapsed();
            transport.Verify(t => t.Send(It.IsAny<ArraySegment<byte>>()), Times.Exactly(3));
            telemetry.Verify(t => t.MetricSent(), Times.Exactly(3));

            s.Add(5);
            s.Add(6);
            s.Add(7);
            s.Add(8);
            s.Add(5);
            timer.TriggerElapsed();
            transport.Verify(t => t.Send(It.IsAny<ArraySegment<byte>>()), Times.Exactly(7));
            telemetry.Verify(t => t.MetricSent(), Times.Exactly(7));
        }

        [Test]
        public void SetShouldFlushOnDispose()
        {
            var transport = new Mock<ITransport>();
            var telemetry = new Mock<ITelemetry>();
            var timer = new ManualTimer();
            var s = new Set(transport.Object, telemetry.Object, timer, MetricName, null);

            s.Add(0);
            s.Dispose();
            transport.Verify(t => t.Send(It.IsAny<ArraySegment<byte>>()), Times.Exactly(1));
            telemetry.Verify(t => t.MetricSent(), Times.Exactly(1));
        }
    }
}
