namespace Dandy.Lms.PF2.FirmwareUpdate
{
    /// <summary>
    /// STM32 CPU protection level.
    /// </summary>
    public enum FlashProtectionLevel : byte
    {
        /// <summary>
        /// No protection
        /// </summary>
        None,

        /// <summary>
        /// Read protection
        /// </summary>
        Read,

        /// <summary>
        /// No debug
        /// </summary>
        NoDebug,
    }
}
