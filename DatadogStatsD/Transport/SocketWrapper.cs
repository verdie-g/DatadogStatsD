using System;
using System.Net;
using System.Net.Sockets;

namespace DatadogStatsD.Transport
{
    internal class SocketWrapper : ISocket
    {
        private readonly EndPoint _endpoint;
        private Socket _underlyingSocket;

        public SocketWrapper(EndPoint endpoint)
        {
            _endpoint = endpoint;
            _underlyingSocket = CreateSocket(_endpoint);
        }

        public void Send(ArraySegment<byte> buffer)
        {
            if (!_underlyingSocket.Connected)
            {
                Dispose();
                _underlyingSocket = CreateSocket(_endpoint);
            }

#if NETSTANDARD2_0
            _underlyingSocket.Send(new[] { buffer });
#else
            _underlyingSocket.Send(buffer);
#endif
        }

        public void Dispose()
        {
            _underlyingSocket?.Dispose();
        }

        private static Socket CreateSocket(EndPoint endpoint)
        {
            var socket = endpoint.AddressFamily switch
            {
                AddressFamily.Unix => new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.IP),
                _ =>  new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp),
            };
            socket.Connect(endpoint);
            return socket;
        }
    }
}
