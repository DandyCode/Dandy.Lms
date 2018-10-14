using System;

namespace Dandy.Lms.Internal
{
    /// <summary>
    /// Represents a command that can be sent to a Powered Up device that has
    /// been booted into the firmware update mode.
    /// </summary>
    /// <typeparam name="T">The parameter(s) of the reply message.</typeparam>
    public sealed class Command<T>
    {
        private readonly ReplyParser replyParser;

        /// <summary>
        /// Gets the raw message data for this command.
        /// </summary>
        public ReadOnlyMemory<byte> Message { get; }

        /// <summary>
        /// Gets the reply requirements for this command.
        /// </summary>
        public RequestTypes RequestTypes { get; }

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
        /// <param name="replyParser"></param>
        public Command(ReadOnlyMemory<byte> message, RequestTypes replyRequirement, ReplyParser replyParser)
        {
            Message = message;
            RequestTypes = replyRequirement;
            this.replyParser = replyParser ?? throw new ArgumentNullException(nameof(replyParser));
        }

        /// <summary>
        /// Parses the raw reply data.
        /// </summary>
        /// <param name="reply">The reply.</param>
        /// <returns></returns>
        public T ParseReplay(ReadOnlySpan<byte> reply) => replyParser(reply);
    }
}
