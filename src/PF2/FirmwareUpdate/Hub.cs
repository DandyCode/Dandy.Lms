using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using static Dandy.Lms.PF2.FirmwareUpdate.CommandFactory;

namespace Dandy.Lms.PF2.FirmwareUpdate
{
    /// <summary>
    /// Represents a PF2 hub running in firmware update mode.
    /// </summary>
    public sealed class Hub
    {
        private readonly BLEConnection connection;

        /// <summary>
        /// Gets a unique identifer for the hub (the Bluetooth address).
        /// </summary>
        public string Id => connection.Address.ToString();

        /// <summary>
        /// Gets the name of the hub.
        /// </summary>
        public string Name => connection.Name;

        /// <summary>
        /// Creates a new instance of a Powered Up hub device.
        /// </summary>
        /// <param name="connection">The Bluetooth LE connection.</param>
        public Hub(BLEConnection connection)
        {
            this.connection = connection ?? throw new ArgumentNullException(nameof(connection));
        }

        /// <summary>
        /// Gets information about the hub.
        /// </summary>
        /// <returns>A <see cref="Task{TResult}" /> that contains the type of hub and the
        /// bootloader firmware version.</returns>
        public async Task<(HubType hubType, Version fwVersion)> GetDeviceInfoAsync()
        {
            var (fwVersion, startAddress, endAddress, hubType) = await connection.SendCommandAsync(DeviceInfo());
            return (hubType, fwVersion);
        }

        /// <summary>
        /// Flashes a firmware file to the hub.
        /// </summary>
        /// <param name="firmware">The raw firmware data.</param>
        /// <param name="progress">Optional progress callback.</param>
        /// <returns>A <see cref="Task"/>.</returns>
        /// <remarks>
        /// The first callback to <paramref name="progress"/> will have <c>erasing</c> set to
        /// <see langword="true"/>. This takes about 1 second. The remaining callbacks will
        /// occur for each 14 byte chunk of the file with <c>erasing</c> set to <see langword="false"/>
        /// and <c>bytes</c> set to the number of bytes transfered so far. In the final call,
        /// <c>bytes</c> should equal the size of <paramref name="firmware"/>.
        /// </remarks>
        public async Task FlashFirmware(ReadOnlyMemory<byte> firmware, IProgress<(bool erasing, int bytes)> progress = null)
        {
            // first, need to get the starting address
            var (_, startAddress, endAddress, _) = await connection.SendCommandAsync(DeviceInfo());

            if (startAddress + firmware.Length >= endAddress + 1) {
                throw new ArgumentOutOfRangeException("Firmware is too big", nameof(firmware));
            }

            var ok = await connection.SendCommandAsync(ValidateFirmwareSize(firmware.Length));

            if (!ok) {
                // this shouldn't happen since we checked the size before, but just in case...
                throw new ArgumentOutOfRangeException("Firmware is too big", nameof(firmware));
            }

            progress?.Report((true, 0));

            ok = await connection.SendCommandAsync(EraseFirmware());
            if (!ok) {
                throw new IOException("Failed to erase flash memory");
            }

            progress?.Report((false, 0));

            // data can only be sent 14 bytes at a time
            var chunks = (uint)firmware.Length / 14;
            var finalChunkSize = firmware.Length % 14;

            // we wait for reply after last command; last command cannot have empty data
            if (finalChunkSize == 0) {
                chunks--;
                finalChunkSize = 14;
            }

            for (var i = 0U; i < chunks; i++) {
                await connection.SendCommandAsync(WriteFirmware(startAddress + i * 14,
                    firmware.Slice((int)i * 14, 14).Span, false), false);
                progress?.Report((false, (int)i * 14));
            }

            await connection.SendCommandAsync(WriteFirmware(startAddress + chunks * 14,
                firmware.Slice((int)chunks * 14, finalChunkSize).Span, true));
            progress?.Report((false, firmware.Length));
        }

        /// <summary>
        /// Reboots the hub.
        /// </summary>
        /// <returns>A <see cref="Task"/> indicating if the command was sucessfully sent.</returns>
        public Task RebootAsync()
        {
            return connection.SendCommandAsync(Reboot(), false);
        }

        /// <summary>
        /// Disconnects the Bluetooth LE connection.
        /// </summary>
        /// <returns>A <see cref="Task"/> indicating if the command was sucessfully sent.</returns>
        public Task DisconnectAsync()
        {
            return connection.SendCommandAsync(Disconnect(), false);
        }
    }
}
