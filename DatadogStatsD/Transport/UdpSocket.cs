using System;
using System.Net;
using System.Net.Sockets;

namespace DatadogStatsD.Transport
{
    internal class UdpSocket : ISocket
    {
        private readonly Socket _socket;

        public UdpSocket()
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        }

        public void Connect(IPAddress ipAddress, int port)
        {
            _socket.Connect(ipAddress, port);
        }

        public int Send(ArraySegment<byte> buffer, SocketFlags flags)
        {
            return _socket.Send(buffer, flags);
        }

        public void Dispose()
        {
            _socket.Dispose();
        }
    }
}