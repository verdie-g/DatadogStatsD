using System.Net.Sockets;
using DatadogStatsD.Transport;
using NUnit.Framework;

namespace DatadogStatsD.Test
{
    public class UdsSocketTest
    {
        [Test]
        public void ShouldThrowIfSocketDoesntExist()
        {
            Assert.Throws<SocketException>(() => new UdsSocket("toto"));
        }
    }
}