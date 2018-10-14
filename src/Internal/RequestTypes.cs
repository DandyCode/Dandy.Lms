using System;

namespace Dandy.Lms.Internal
{
    /// <summary>
    /// Indicates the allowable request types for a <see cref="Command{T}"/>.
    /// </summary>
    [Flags]
    public enum RequestTypes
    {
        /// <summary>
        /// Making a request where the brick does not send a reply is allowed.
        /// </summary>
        NoReply = 1 << 0,

        /// <summary>
        /// Making a request where the brick sends one (and only one) reply is allowd.
        /// </summary>
        Reply = 1 << 1,

        /// <summary>
        /// Making a request where the brick sends more than one reply is allowed.
        /// </summary>
        Subscribe = 1<< 2,
    }
}
