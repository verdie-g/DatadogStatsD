using System;
using System.Threading.Tasks;

namespace DatadogStatsD.Transport
{
    internal interface ISocket : IDisposable
    {
        void Send(ArraySegment<byte> buffer);
    }
}