using System;
using System.Buffers;
using System.Diagnostics;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace DatadogStatsD.Transport
{
    /// <summary>
    /// Wraps an <see cref="ISocket"/> to buffer messages sent and join them with a newline.
    /// </summary>
    internal class NonBlockingBufferedTransport : ITransport
    {
        private readonly ISocket _socket;
        private readonly int _maxBufferingSize;
        private readonly TimeSpan _maxBufferingTime;
        private readonly Channel<ArraySegment<byte>> _chan;
        private readonly Task _sendBuffersTask;
        private readonly CancellationTokenSource _sendBuffersCancellation;

        public NonBlockingBufferedTransport(ISocket socket, int maxBufferingSize, TimeSpan maxBufferingTime, int maxQueueSize)
        {
            _socket = socket;
            _maxBufferingSize = maxBufferingSize;
            _maxBufferingTime = maxBufferingTime;
            _chan = Channel.CreateBounded<ArraySegment<byte>>(new BoundedChannelOptions(maxQueueSize)
            {
                SingleReader = true,
                SingleWriter = false,
            });
            _sendBuffersCancellation = new CancellationTokenSource();
            _sendBuffersTask = Task.Run(SendBuffers, _sendBuffersCancellation.Token);
        }

        public event Action<int> OnPacketSent = size => {};
        public event Action<int, bool> OnPacketDropped = (size, queue) => {};

        public void Send(ArraySegment<byte> buffer)
        {
            if (!_sendBuffersTask.IsCompleted && _chan.Writer.TryWrite(buffer))
            {
                return;
            }

            OnPacketDropped(buffer.Count, true);
            ArrayPool<byte>.Shared.Return(buffer.Array);
        }

        public void Dispose()
        {
            _sendBuffersCancellation.Cancel();
            try
            {
                _sendBuffersTask.GetAwaiter().GetResult();
            }
            catch (OperationCanceledException)
            {
            }
            _sendBuffersTask.Dispose();
            _sendBuffersCancellation.Dispose();
            _socket.Dispose();
        }

        private async Task SendBuffers()
        {
            var bufferingCtx = new BufferingContext(_maxBufferingSize, _maxBufferingTime, _sendBuffersCancellation.Token);

            while (true)
            {
                ArraySegment<byte> buffer;
                if (_sendBuffersCancellation.IsCancellationRequested) // disposing
                {
                    if (!_chan.Reader.TryRead(out buffer))
                    {
                        Flush(bufferingCtx);
                        return;
                    }
                }
                else
                {
                    // the try block was not extract in its own method to avoid the cost of a new async state machine
                    try
                    {
                        bufferingCtx.EnableTimeout();
                        buffer = await _chan.Reader.ReadAsync(bufferingCtx.BufferingCancellation);
                        // make sure it doesn't get cancelled so it can be reused in the next iteration
                        bufferingCtx.DisableTimeout();
                    }
                    catch (OperationCanceledException) // timeout reached or disposing
                    {
                        if (!_sendBuffersCancellation.IsCancellationRequested) // timeout
                        {
                            Flush(bufferingCtx);
                        }

                        // continue in both case. If timeout, reloop to retry getting a value. If disposing reloop to dequeue all buffers
                        continue;
                    }
                }

                if (buffer.Count > _maxBufferingSize)
                {
                    SendBuffer(buffer);
                    ArrayPool<byte>.Shared.Return(buffer.Array);
                    continue;
                }

                if (!bufferingCtx.Fits(buffer))
                {
                    Flush(bufferingCtx);
                }

                bufferingCtx.Append(buffer);
                ArrayPool<byte>.Shared.Return(buffer.Array);
            }
        }

        private void Flush(BufferingContext bufferingCtx)
        {
            if (bufferingCtx.Segment.Count != 0)
            {
                SendBuffer(bufferingCtx.Segment);
            }

            bufferingCtx.Reset();
        }

        private void SendBuffer(ArraySegment<byte> buffer)
        {
            try
            {
                _socket.Send(buffer);
                OnPacketSent(buffer.Count);
            }
            catch
            {
                OnPacketDropped(buffer.Count, false);
            }
        }

        private class BufferingContext
        {
            private readonly int _maxBufferingSize;
            private readonly TimeSpan _maxBufferingTime;
            private readonly CancellationToken _cancellationToken; // global cancellation
            private readonly byte[] _buffer;
            private readonly Stopwatch _stopwatch;
            private CancellationTokenSource _bufferingCancellation;
            private int _size;

            public ArraySegment<byte> Segment => new ArraySegment<byte>(_buffer, 0, _size - 1); // -1 for extra '\n'
            public CancellationToken BufferingCancellation => _bufferingCancellation.Token;

            public BufferingContext(int maxBufferingSize, TimeSpan maxBufferingTime, CancellationToken cancellationToken)
            {
                _maxBufferingSize = maxBufferingSize;
                _maxBufferingTime = maxBufferingTime;
                _cancellationToken = cancellationToken;
                _buffer = new byte[maxBufferingSize + 1]; // +1 for extra '\n'
                _stopwatch = new Stopwatch();
                _bufferingCancellation = CancellationTokenSource.CreateLinkedTokenSource(_cancellationToken);
                _size = 0;

                _stopwatch.Start();
            }

            public void Append(ArraySegment<byte> buffer)
            {
                Array.Copy(buffer.Array, 0, _buffer, _size, buffer.Count);
                _size += buffer.Count;

                if (_size <= _maxBufferingSize)
                {
                    _buffer[_size] = (byte)'\n';
                    _size += 1;
                }
            }

            public bool Fits(ArraySegment<byte> buffer)
            {
                return _size + buffer.Count <= _maxBufferingSize;
            }

            public void Reset()
            {
                _size = 0;
                _stopwatch.Restart();
            }

            public void EnableTimeout()
            {
                if (_bufferingCancellation.IsCancellationRequested)
                {
                    _bufferingCancellation.Dispose();
                    _bufferingCancellation = CancellationTokenSource.CreateLinkedTokenSource(_cancellationToken);
                }

                double remainingBufferingTime = Math.Max(0, _maxBufferingTime.TotalMilliseconds - _stopwatch.Elapsed.TotalMilliseconds);
                _bufferingCancellation.CancelAfter((int)remainingBufferingTime);
            }

            public void DisableTimeout()
            {
                // Timeout.Infinite won't work because it would delete the underlying timer
                _bufferingCancellation.CancelAfter(int.MaxValue);
            }
        }
    }
}