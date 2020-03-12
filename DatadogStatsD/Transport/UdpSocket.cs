using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace DatadogStatsD.Transport
{
    internal class UdpSocket : ISocket
    {
        private readonly Socket _underlyingSocket;

        public UdpSocket(string host, int port)
        {
            var address = Dns.GetHostEntry(host).AddressList.First(a => a.AddressFamily == AddressFamily.InterNetwork);
            var endpoint = new IPEndPoint(address, port);

            _underlyingSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            _underlyingSocket.Connect(endpoint);

            _underlyingSocket.Send(new ArraySegment<byte>(Array.Empty<byte>())); // the first one get the potential ICMP error
            _underlyingSocket.Send(new ArraySegment<byte>(Array.Empty<byte>())); // the second throws if there was an error
            // passing this test doesn't mean a socket is listening https://serverfault.com/a/416269
        }

        public Task SendAsync(ArraySegment<byte> buffer)
        {
            return _underlyingSocket.SendAsync(buffer, SocketFlags.None);
        }

        public void Dispose()
        {
            _underlyingSocket.Dispose();
        }
    }
}