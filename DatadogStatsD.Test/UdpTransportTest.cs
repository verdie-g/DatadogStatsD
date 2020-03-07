using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using DatadogStatsD.Transport;
using Moq;
using NUnit.Framework;

namespace DatadogStatsD.Test
{
    public class UdpTransportTest
    {
        private readonly ArraySegment<byte> buffer508 = CreateBuffer(508);
        private readonly ArraySegment<byte> buffer254 = CreateBuffer(254);
        private readonly ArraySegment<byte> buffer253 = CreateBuffer(253);

        private static ArraySegment<byte> CreateBuffer(int size)
        {
            var buffer = ArrayPool<byte>.Shared.Rent(size);
            for (int i = 0; i < size; i += 1)
            {
                buffer[i] = (byte)i;
            }

            return new ArraySegment<byte>(buffer, 0, size);
        }

        [Test]
        public void ShouldThrowIfNoListener()
        {
            Assert.Throws<SocketException>(() => new UdpTransport("localhost", 1111));
        }

        [Test]
        public async Task TestFlush_1x508()
        {
            var socket = new Mock<ISocket>();
            var buffers = new BlockingCollection<ArraySegment<byte>>(new ConcurrentQueue<ArraySegment<byte>>())
            {
                buffer508
            };
            var cts = new CancellationTokenSource();

            Task.Run(() => UdpTransport.SendBuffers(socket.Object, buffers, cts.Token));
            await Task.Delay(2500);
            VerifySendCalledWithBufferOfSize(socket, 508, Times.Once);
            cts.Cancel();
        }

        [Test]
        public async Task TestFlush_1x254_1x253()
        {
            var socket = new Mock<ISocket>();
            var buffers = new BlockingCollection<ArraySegment<byte>>(new ConcurrentQueue<ArraySegment<byte>>())
            {
                buffer254,
                buffer253,
            };
            var cts = new CancellationTokenSource();

            Task.Run(() => UdpTransport.SendBuffers(socket.Object, buffers, cts.Token));
            await Task.Delay(2500);
            VerifySendCalledWithBufferOfSize(socket, buffer254.Count + 1 + buffer253.Count, Times.Once);
            cts.Cancel();
        }

        [Test]
        public async Task TestFlush_1x253()
        {
            var socket = new Mock<ISocket>();
            var buffers = new BlockingCollection<ArraySegment<byte>>(new ConcurrentQueue<ArraySegment<byte>>())
            {
                buffer253,
            };
            var cts = new CancellationTokenSource();

            Task.Run(() => UdpTransport.SendBuffers(socket.Object, buffers, cts.Token));
            await Task.Delay(2500);
            VerifySendCalledWithBufferOfSize(socket, buffer253.Count, Times.Once);
            cts.Cancel();
        }

        [Test]
        public async Task TestFlush_1x254_1x508()
        {
            var socket = new Mock<ISocket>();
            var buffers = new BlockingCollection<ArraySegment<byte>>(new ConcurrentQueue<ArraySegment<byte>>())
            {
                buffer254,
                buffer508,
            };
            var cts = new CancellationTokenSource();

            Task.Run(() => UdpTransport.SendBuffers(socket.Object, buffers, cts.Token));
            await Task.Delay(500);
            VerifySendCalledWithBufferOfSize(socket, buffer254.Count, Times.Once);
            await Task.Delay(2000);
            VerifySendCalledWithBufferOfSize(socket, buffer508.Count, Times.Once);
            cts.Cancel();
        }

        private void VerifySendCalledWithBufferOfSize(Mock<ISocket> socketMock, int size, Func<Times> times)
        {
            socketMock.Verify(
                s => s.Send(It.Is<ArraySegment<byte>>(b => b.Count == size), It.IsAny<SocketFlags>()),
                times);
        }
    }
}