using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace DatadogStatsD.Transport
{
    internal class UdsSocket : ISocket
    {
        private readonly UnixDomainSocketEndPoint _endpoint;
        private Socket _underlyingSocket;

        public UdsSocket(string path)
        {
            _endpoint = new UnixDomainSocketEndPoint(path);
            _underlyingSocket = CreateUnixSocket(_endpoint);
        }

        public void Send(ArraySegment<byte> buffer)
        {
            if (!_underlyingSocket.Connected)
            {
                Dispose();
                _underlyingSocket = CreateUnixSocket(_endpoint);
            }

            _underlyingSocket.Send(buffer, SocketFlags.None);
        }

        public void Dispose()
        {
            _underlyingSocket?.Dispose();
        }

        private static Socket CreateUnixSocket(EndPoint endpoint)
        {
            var socket = new Socket(AddressFamily.Unix, SocketType.Dgram, ProtocolType.IP);
            socket.Connect(endpoint);
            return socket;
        }
    }
}