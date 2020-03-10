using System.Net.Sockets;
using DatadogStatsD.Transport;
using NUnit.Framework;

namespace DatadogStatsD.Test
{
    public class UdpSocketTest
    {
        [Test]
        public void ShouldThrowIfNoListener()
        {
            Assert.Throws<SocketException>(() => new UdpSocket("localhost", 1111));
        }
    }
}