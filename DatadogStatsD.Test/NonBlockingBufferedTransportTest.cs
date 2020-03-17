using System;
using System.Buffers;
using System.Net.Sockets;
using System.Threading.Tasks;
using DatadogStatsD.Transport;
using Moq;
using NUnit.Framework;

namespace DatadogStatsD.Test
{
    public class NonBlockingBufferedTransportTest
    {
        [Test]
        public async Task MessagesLargerThanMaxBufferingSizeAreSentDirectly()
        {
            var socket = new Mock<ISocket>();
            var transport = new NonBlockingBufferedTransport(socket.Object, 10, TimeSpan.FromSeconds(5), 500);

            transport.Send(CreateBuffer(11));
            await Task.Delay(15); // wait dequeue
            VerifySendCalledWithBufferOfSize(socket, 11, Times.Once());
        }

        [Test]
        public async Task MessagesLargerThanMaxBufferingSizeAreSentDirectlyEvenIfAMessageIsAlreadyBuffered()
        {
            var socket = new Mock<ISocket>();
            var transport = new NonBlockingBufferedTransport(socket.Object, 10, TimeSpan.FromSeconds(5), 500);

            transport.Send(CreateBuffer(9));
            transport.Send(CreateBuffer(11));
            await Task.Delay(15); // wait dequeue
            VerifySendCalledWithBufferOfSize(socket, 9, Times.Never());
            VerifySendCalledWithBufferOfSize(socket, 11, Times.Once());
        }

        [Test]
        public async Task OneMessageEqualsToMaxBufferingSizeIsSentAfterBufferingTimeout()
        {
            var socket = new Mock<ISocket>();
            var transport = new NonBlockingBufferedTransport(socket.Object, 10, TimeSpan.FromMilliseconds(1000), 500);

            transport.Send(CreateBuffer(10));
            await Task.Delay(15);
            VerifySendCalledWithBufferOfSize(socket, 10, Times.Never());
            await Task.Delay(1500); // wait buffering timeout
            VerifySendCalledWithBufferOfSize(socket, 10, Times.Once());
        }

        [Test]
        public async Task TwoMessagesEqualToMaxBufferingSizeAreSentAfterBufferingTimeout()
        {
            var socket = new Mock<ISocket>();
            var transport = new NonBlockingBufferedTransport(socket.Object, 10, TimeSpan.FromMilliseconds(1000), 500);

            transport.Send(CreateBuffer(5));
            transport.Send(CreateBuffer(4));
            await Task.Delay(15);
            VerifySendCalledWithBufferOfSize(socket, 10, Times.Never());
            await Task.Delay(1500); // wait buffering timeout
            VerifySendCalledWithBufferOfSize(socket, 10, Times.Once());
        }

        [Test]
        public async Task OneMessageEqualsToMaxBufferingSizeIsSentWhenAnotherIsDequeued()
        {
            var socket = new Mock<ISocket>();
            var transport = new NonBlockingBufferedTransport(socket.Object, 10, TimeSpan.FromMilliseconds(1000), 500);

            transport.Send(CreateBuffer(10));
            await Task.Delay(15);
            VerifySendCalledWithBufferOfSize(socket, 10, Times.Never());
            transport.Send(CreateBuffer(9));
            await Task.Delay(15); // wait dequeue
            VerifySendCalledWithBufferOfSize(socket, 10, Times.Once());
            VerifySendCalledWithBufferOfSize(socket, 9, Times.Never());
        }

        [Test]
        public async Task TwoMessagesEqualToMaxBufferingSizeAreSentWhenAnotherIsDequeued()
        {
            var socket = new Mock<ISocket>();
            var transport = new NonBlockingBufferedTransport(socket.Object, 10, TimeSpan.FromMilliseconds(1000), 500);

            transport.Send(CreateBuffer(5));
            transport.Send(CreateBuffer(4));
            await Task.Delay(15);
            VerifySendCalledWithBufferOfSize(socket, 10, Times.Never());
            transport.Send(CreateBuffer(9));
            await Task.Delay(15); // wait dequeue
            VerifySendCalledWithBufferOfSize(socket, 10, Times.Once());
            VerifySendCalledWithBufferOfSize(socket, 9, Times.Never());
        }

        [Test]
        public async Task OneMessageLessThanMaxBufferingSizeIsSentAfterBufferingTimeout()
        {
            var socket = new Mock<ISocket>();
            var transport = new NonBlockingBufferedTransport(socket.Object, 10, TimeSpan.FromMilliseconds(1000), 500);

            transport.Send(CreateBuffer(9));
            await Task.Delay(15);
            VerifySendCalledWithBufferOfSize(socket, 9, Times.Never());
            await Task.Delay(1500); // wait buffering timeout
            VerifySendCalledWithBufferOfSize(socket, 9, Times.Once());
        }

        [Test]
        public async Task WhenNoMessageEnqueueNothingShouldBeSent()
        {
            var socket = new Mock<ISocket>();
            var transport = new NonBlockingBufferedTransport(socket.Object, 10, TimeSpan.FromMilliseconds(50), 500);

            await Task.Delay(750); // wait many buffering timeouts
            VerifySendCalledWithBufferOfSize(socket, It.IsAny<int>(), Times.Never());
        }

        [Test]
        public async Task ResumesCorrectlyAfterASocketException()
        {
            var socket = new Mock<ISocket>();
            socket.SetupSequence(s => s.SendAsync(It.IsAny<ArraySegment<byte>>()))
                .Throws(new SocketException())
                .Returns(Task.CompletedTask);

            var transport = new NonBlockingBufferedTransport(socket.Object, 10, TimeSpan.FromMilliseconds(1000), 2);

            transport.Send(CreateBuffer(10));
            await Task.Delay(1500); // wait buffering timeout
            transport.Send(CreateBuffer(10));
            await Task.Delay(4000); // wait two buffering timeout
            VerifySendCalledWithBufferOfSize(socket, 10, Times.Exactly(2));
        }

        [Test]
        public void DisposeReturnsCorrectly()
        {
            var socket = new Mock<ISocket>();
            var transport = new NonBlockingBufferedTransport(socket.Object, 20, TimeSpan.FromMilliseconds(1000), 2);
            transport.Dispose();
        }

        private ArraySegment<byte> CreateBuffer(int size)
        {
            var buffer = ArrayPool<byte>.Shared.Rent(size);
            for (int i = 0; i < size; i += 1)
            {
                buffer[i] = (byte)i;
            }

            return new ArraySegment<byte>(buffer, 0, size);
        }

        private void VerifySendCalledWithBufferOfSize(Mock<ISocket> socketMock, int size, Times times)
        {
            socketMock.Verify(
                s => s.SendAsync(It.Is<ArraySegment<byte>>(b => b.Count == size)),
                times);
        }
    }
}