using System;

namespace Dandy.Lms.Nxt.Commands
{
    public enum ReplyStatus : byte
    {
        Success = 0x00,
        NoMoreHandles = 0x81,
        NoSpace = 0x82,
        NoMoreFiles = 0x83,
        EndOfFileExpected = 0x84,
        EndOfFile = 0x85,
        NotALinearFile = 0x86,
        FileNotFound = 0x87,
        HandleAlreadyClosed = 0x88,
        NoLinearSpace = 0x89,
        UndefinedError = 0x8A,
        FileIsBusy = 0x8B,
        NoWriteBuffers = 0x8C,
        AppendNotPossible = 0x8D,
        FileIsFull = 0x8E,
        FileExists = 0x8F,
        ModuleNotFound = 0x90,
        OutOfBoundary = 0x91,
        IllegalFileName = 0x92,
        IllegalHandle = 0x93,
    }

    public sealed class CommandFailedException : Exception
    {
        public ReplyStatus Status { get; }

        public CommandFailedException(ReplyStatus status)
        {
            Status = status;
        }
    }
}
