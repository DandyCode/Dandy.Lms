namespace Dandy.Lms.PF2.FirmwareUpdate
{
    /// <summary>
    /// Powered Up hub type.
    /// </summary>
    public enum HubType
    {
        /// <summary>
        /// LEGO Move Hub with 2 I/O ports (BOOST Move Hub).
        /// </summary>
        MoveHub = 0x40,

        /// <summary>
        /// Powered Up Smart Hub with 2 I/O ports (HUB #4).
        /// </summary>
        SmartHub = 0x41,

        /// <summary>
        /// Remote control handset.
        /// </summary>
        Handset = 0x42,
    }
}
