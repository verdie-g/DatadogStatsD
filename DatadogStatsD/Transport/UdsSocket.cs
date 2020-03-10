using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace DatadogStatsD.Transport
{
    internal class UdsSocket : ISocket
    {
        private readonly Socket _underlyingSocket;

        public UdsSocket(string path)
        {
            var endpoint = new UnixDomainSocketEndPoint(path);
            _underlyingSocket = new Socket(AddressFamily.Unix, SocketType.Dgram, ProtocolType.IP)
            {
                NoDelay = true,
            };
            _underlyingSocket.Connect(endpoint);
        }

        public void Send(ArraySegment<byte> buffer)
        {
            _underlyingSocket.Send(buffer);
        }

        public void Dispose()
        {
            _underlyingSocket.Dispose();
        }
    }
}