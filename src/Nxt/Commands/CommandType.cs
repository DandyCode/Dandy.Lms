using System;

namespace Dandy.Lms.Nxt.Commands
{
    enum CommandType : byte
    {
        Direct = 0x0,
        System = 0x1,
        Reply = 0x2,
        DirectNoReply = 0x80,
        SystemNoReply = 0x81,
    }

    static class CommandTypeExtensions
    {
        public static bool RequiresReply(this CommandType type)
        {
            return type.HasFlag(CommandType.DirectNoReply);
        }
    }
}
