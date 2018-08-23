using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dandy.Devices.Usb;
using Dandy.Lms.Nxt.Commands;

namespace Dandy.Lms.Nxt.IO
{
    public abstract class NxtConnection : IDisposable
    {
        private readonly ConcurrentQueue<Func<Task>> messageQueue;
        private readonly AutoResetEvent workerEvent;
        private readonly Task worker;
        private bool disposed;

        public string BrickName => throw new NotImplementedException();

        protected NxtConnection()
        {
            messageQueue = new ConcurrentQueue<Func<Task>>();
            workerEvent = new AutoResetEvent(false);

            // background task to synchronize messages
            worker = Task.Run(async () => {
                while (!disposed) {
                    workerEvent.WaitOne();
                    while (messageQueue.TryDequeue(out var msg)) {
                        await msg();
                    }
                }
            });
        }

        protected abstract Task WriteAsync(ReadOnlyMemory<byte> data);

        protected abstract Task<ReadOnlyMemory<byte>> ReadAsync(int size);

        /// <summary>
        /// Disconnect the I/O connection. All outstanding messages will be
        /// processed synchronously.
        /// </summary>
        /// <remarks>
        /// Overriding methods must call this base method before freeing any
        /// resources.
        /// </remarks>
        public virtual void Dispose()
        {
            disposed = true;

            // drain the message queue
            workerEvent.Set();
            worker.Wait();
        }

        public Task<TReply> SendCommandAsync<TReply>(Command<TReply> command)
        {
            if (disposed) {
                throw new ObjectDisposedException(null);
            }

            var source = new TaskCompletionSource<TReply>(command);

            async Task doMessage()
            {
                try {
                    await WriteAsync(command.Payload);
                    var replyData = await ReadAsync(command.ReplySize);
                    var reply = command.ParseReply(replyData.Span);
                    source.SetResult(reply);
                }
                catch (Exception ex) {
                    source.TrySetException(ex);
                }
            }

            messageQueue.Enqueue(doMessage);
            workerEvent.Set();
            return source.Task;
        }
    }
}
