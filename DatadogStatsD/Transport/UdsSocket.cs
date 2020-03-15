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
            _underlyingSocket = new Socket(AddressFamily.Unix, SocketType.Dgram, ProtocolType.IP);
            _underlyingSocket.Connect(endpoint);
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