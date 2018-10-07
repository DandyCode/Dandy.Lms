namespace Dandy.Lms.Internal
{
    /// <summary>
    /// Indicates if a reply can be requested for a <see cref="Command{T}"/>.
    /// </summary>
    public enum ReplyRequirement
    {
        /// <summary>
        /// Requesting a reply is always required.
        /// </summary>
        Always,

        /// <summary>
        /// Requesting a reply is optional.
        /// </summary>
        Optional,

        /// <summary>
        /// Requesting a reply is forbidden.
        /// </summary>
        Never,
    }
}
