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
            var ex = Assert.Catch(() => new UdsSocket("toto"));
            Assert.IsInstanceOf<SocketException>(ex);
        }
    }
}