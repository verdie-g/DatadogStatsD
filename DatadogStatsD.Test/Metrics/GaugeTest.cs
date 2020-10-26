using System;
using System.Collections.Generic;
using DatadogStatsD.Metrics;
using DatadogStatsD.Telemetering;
using DatadogStatsD.Transport;
using Moq;
using NUnit.Framework;

namespace DatadogStatsD.Test.Metrics
{
    public class GaugeTest
    {
        private const string MetricName = "toto";
        private static readonly IList<string> Tags = new[] { "abc:def", "ghi" };

        [Test]
        public void EvaluatorShouldBeCalledForEachTick()
        {
            var transport = new Mock<ITransport>();
            var telemetry = new Mock<ITelemetry>();
            var timer = new ManualTimer();
            int evaluatorCalled = 0;
            var c = new Gauge(transport.Object, telemetry.Object, timer, MetricName, () => evaluatorCalled++, Tags);

            timer.TriggerElapsed();
            Assert.AreEqual(1, evaluatorCalled);
            transport.Verify(t => t.Send(It.IsAny<ArraySegment<byte>>()), Times.Exactly(1));
            telemetry.Verify(t => t.MetricSent(), Times.Exactly(1));

            timer.TriggerElapsed();
            Assert.AreEqual(2, evaluatorCalled);
            transport.Verify(t => t.Send(It.IsAny<ArraySegment<byte>>()), Times.Exactly(2));
            telemetry.Verify(t => t.MetricSent(), Times.Exactly(2));
        }

        [Test]
        public void NothingShouldBeSentIfEvaluatorThrows()
        {
            var transport = new Mock<ITransport>();
            var telemetry = new Mock<ITelemetry>();
            var timer = new ManualTimer();

            double Evaluator() => throw new Exception();
            var c = new Gauge(transport.Object, telemetry.Object, timer, MetricName, Evaluator, Tags);

            timer.TriggerElapsed();
            transport.Verify(t => t.Send(It.IsAny<ArraySegment<byte>>()), Times.Exactly(0));
            telemetry.Verify(t => t.MetricSent(), Times.Exactly(0));

            timer.TriggerElapsed();
            transport.Verify(t => t.Send(It.IsAny<ArraySegment<byte>>()), Times.Exactly(0));
            telemetry.Verify(t => t.MetricSent(), Times.Exactly(0));
        }

        [Test]
        public void EvaluatorShouldBeCalledOnDispose()
        {
            var transport = new Mock<ITransport>();
            var telemetry = new Mock<ITelemetry>();
            var timer = new ManualTimer();
            int evaluatorCalled = 0;
            var c = new Gauge(transport.Object, telemetry.Object, timer, MetricName, () => evaluatorCalled++, Tags);

            c.Dispose();
            Assert.AreEqual(1, evaluatorCalled);
        }
    }
}
