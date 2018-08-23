using System;

namespace Dandy.Lms.Nxt.Commands
{
    public sealed class Command<TReply>
    {
        public delegate TReply ReplyParser(ReadOnlySpan<byte> response);

        private readonly ReplyParser replyParser;

        internal Command(ReadOnlyMemory<byte> payload, int replySize, ReplyParser replyParser)
        {
            Payload = payload;
            ReplySize = replySize;
            this.replyParser = replyParser;
        }

        internal CommandType CommandType => (CommandType)Payload.Span[0];

        internal byte CommandCode => Payload.Span[1];

        internal ReadOnlyMemory<byte> Payload { get; }
        public int ReplySize { get; }

        internal TReply ParseReply(ReadOnlySpan<byte> data)
        {
            if (data[0] != Payload.Span[0]) {
                throw new ArgumentException("CommandType does not match");
            }
            if (data[1] != Payload.Span[1]) {
                throw new ArgumentException("CommandCode does not match");
            }
            var status = (ReplyStatus)data[2];
            if (status != ReplyStatus.Success) {
                throw new CommandFailedException(status);
            }

            if (replyParser == null) {
                throw new InvalidOperationException("This command does not expect a reply");
            }
            return replyParser(data);
        }
    }
}
