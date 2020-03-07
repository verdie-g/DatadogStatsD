using System;
using System.Net;
using System.Net.Sockets;

namespace DatadogStatsD.Transport
{
    internal interface ISocket : IDisposable
    {
        void Connect(IPAddress ipAddress, int port);
        int Send(ArraySegment<byte> buffer, SocketFlags flags);
    }
}