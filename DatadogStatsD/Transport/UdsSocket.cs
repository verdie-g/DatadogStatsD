using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace DatadogStatsD.Transport
{
    internal class UdsSocket : ISocket
    {
        private readonly UnixDomainSocketEndPoint _endpoint;
        private Socket? _underlyingSocket;

        public UdsSocket(string path)
        {
            _endpoint = new UnixDomainSocketEndPoint(path);
            // sync over async, acceptable at startup time
            CreateSocket().GetAwaiter().GetResult();
        }

        public async Task SendAsync(ArraySegment<byte> buffer)
        {
            if (_underlyingSocket == null || !_underlyingSocket.Connected)
            {
                await CreateSocket();
            }

            await _underlyingSocket.SendAsync(buffer, SocketFlags.None);
        }

        public void Dispose()
        {
            _underlyingSocket?.Dispose();
        }

        private Task CreateSocket()
        {
            Dispose();
            _underlyingSocket = new Socket(AddressFamily.Unix, SocketType.Dgram, ProtocolType.IP);
            return _underlyingSocket.ConnectAsync(_endpoint);
        }
    }
}