using System;

namespace Dandy.Lms.Internal
{
    /// <summary>
    /// Class to be used as a type parameter for commands that never return a reply.
    /// </summary>
    /// <remarks>
    /// No instance of this class is ever created.
    /// </remarks>
    public sealed class NoReply
    {
        NoReply()
        {
            throw new InvalidOperationException();
        }
    }
}
