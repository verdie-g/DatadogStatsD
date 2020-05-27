using System.Net.Sockets;
using DatadogStatsD.Transport;
using NUnit.Framework;

namespace DatadogStatsD.Test
{
    public class SocketWrapperTest
    {
        [Test]
        public void ShouldThrowIfUnixSocketDoesntExist()
        {
            var ex = Assert.Catch(() =>  new SocketWrapper(new UnixDomainSocketEndPoint("toto")));
            Assert.IsInstanceOf<SocketException>(ex);
        }
    }
}