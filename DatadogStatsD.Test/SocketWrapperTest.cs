using System.Net.Sockets;
using DatadogStatsD.Transport;
using NUnit.Framework;

namespace DatadogStatsD.Test
{
    public class SocketWrapperTest
    {
#if !NETCOREAPP2_1
        [Test]
        public void ShouldThrowIfUnixSocketDoesntExist()
        {
            var ex = Assert.Catch(() =>  new SocketWrapper(new UnixDomainSocketEndPoint("toto")));
            Assert.IsInstanceOf<SocketException>(ex);
        }
#endif
    }
}
