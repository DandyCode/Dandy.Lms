using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Text;

namespace Dandy.Lms.Internal
{
    /// <summary>
    /// Represents a command that can be sent to a Powered Up device that has
    /// been booted into the firmware update mode.
    /// </summary>
    /// <typeparam name="T">The parameter(s) of the reply message.</typeparam>
    public sealed class Command<T>
    {
        private readonly int replyLength;
        private readonly ReplyParser replyParser;

        /// <summary>
        /// Gets the raw message data for this command.
        /// </summary>
        public ReadOnlyMemory<byte> Message { get; }

        /// <summary>
        /// Gets the reply requirement for this command.
        /// </summary>
        public ReplyRequirement ReplyRequirement { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="reply"></param>
        /// <returns></returns>
        public delegate T ReplyParser(ReadOnlySpan<byte> reply);

        /// <summary>
        /// Creates a new command.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="replyRequirement"></param>
        /// <param name="replyLength"></param>
        /// <param name="replyParser"></param>
        public Command(ReadOnlyMemory<byte> message, ReplyRequirement replyRequirement, int replyLength, ReplyParser replyParser)
        {
            Message = message;
            ReplyRequirement = replyRequirement;
            this.replyLength = replyLength;
            this.replyParser = replyParser ?? throw new ArgumentNullException(nameof(replyParser));
        }

        /// <summary>
        /// Parses the raw reply data.
        /// </summary>
        /// <param name="reply">The reply.</param>
        /// <returns></returns>
        public T ParseReplay(ReadOnlySpan<byte> reply)
        {
            if (reply.Length == 0) {
                throw new ArgumentOutOfRangeException("Reply is empty", nameof(reply));
            }
            if (reply.Length < replyLength) {
                throw new ArgumentOutOfRangeException("Reply is too short", nameof(reply));
            }
            if (reply[0] != Message.Span[0]) {
                throw new ArgumentException("Reply does not match command", nameof(reply));
            }
            return replyParser(reply);
        }
    }
}
