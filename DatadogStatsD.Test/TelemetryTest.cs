using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using DatadogStatsD.Telemetering;
using DatadogStatsD.Transport;
using Moq;
using NUnit.Framework;

namespace DatadogStatsD.Test
{
    public class TelemetryTest
    {
        private static readonly string Version = FileVersionInfo.GetVersionInfo(typeof(Telemetry).Assembly.Location).ProductVersion;

        [Test]
        public void Test()
        {
            var transport = new Mock<ITransport>();
            var timer = new ManualTimer();
            var telemetry = new Telemetry("udp", transport.Object, timer, Array.Empty<string>());

            telemetry.MetricSent();
            telemetry.EventSent();
            telemetry.ServiceCheckSent();
            telemetry.PacketSent(11);
            telemetry.PacketDropped(3, true);
            telemetry.PacketDropped(7, false);

            timer.TriggerElapsed();

            VerifySent(transport, "metrics", 1);
            VerifySent(transport, "events", 1);
            VerifySent(transport, "service_checks", 1);
            VerifySent(transport, "bytes_sent", 11);
            VerifySent(transport, "bytes_dropped", 10);
            VerifySent(transport, "bytes_dropped_queue", 3);
            VerifySent(transport, "bytes_dropped_writer", 7);
            VerifySent(transport, "packets_sent", 1);
            VerifySent(transport, "packets_dropped", 2);
            VerifySent(transport, "packets_dropped_queue", 1);
            VerifySent(transport, "packets_dropped_writer", 1);

            timer.TriggerElapsed();

            transport.VerifyNoOtherCalls();
        }

        private void VerifySent(Mock<ITransport> transport, string metricName, int val)
        {
            string expectedMetric = $"datadog.dogstatsd.client.{metricName}:{val}|c|#client:cs,client_version:{Version},client_transport:udp";
            byte[] expectedMetricBytes = Encoding.UTF8.GetBytes(expectedMetric);
            transport.Verify(t => t.Send(It.Is<ArraySegment<byte>>(a => a.ToArray().SequenceEqual(expectedMetricBytes))));
        }
    }
}
