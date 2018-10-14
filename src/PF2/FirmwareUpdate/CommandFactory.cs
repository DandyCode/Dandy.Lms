using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Text;
using Dandy.Lms.Internal;

namespace Dandy.Lms.PF2.FirmwareUpdate
{
    /// <summary>
    /// Factory for creating Powered Up firmware updater commands.
    /// </summary>
    public static class CommandFactory
    {
        /// <summary>
        /// Creates a new erase firmware command.
        /// </summary>
        /// <returns>A command with a <see cref="bool"/> type parameter that indicates success.</returns>
        /// <remarks>
        /// This will erase the firmware on the programmable brick.
        /// </remarks>
        public static Command<bool> EraseFirmware()
        {
            var message = new byte[] { 0x11 };

            return new Command<bool>(message, RequestTypes.Reply, reply => {
                return reply[1] == 0;
            });
        }

        /// <summary>
        /// Creates a new write firmware command.
        /// </summary>
        /// <param name="startAddress">The starting flash memory address.</param>
        /// <param name="data">The firmware data to be written.</param>
        /// <param name="final">Set to <see langword="true"/> when this is the final
        /// write and a reply is expected.</param>
        /// <returns>
        /// A command with a reply that contains the checksum and the number of
        ///  bytes that have been written.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if <paramref name="data"/> is > 14 bytes.
        ///</exception>.
        /// <remarks>
        /// The <paramref name="data"/> will be written written to flash memory
        ///  starting at <paramref name="startAddress"/>.
        /// <paramref name="data"/> is limited to 14 bytes per command.
        ///
        /// <see cref="EraseFirmware"/> must be called first before writing
        /// firmware. <paramref name="startAddress"/> must be in the range
        /// returned by the <see cref="DeviceInfo"/> command.
        ///
        /// The reply will contain the checksum and new starting address for the next command
        /// (<paramref name="startAddress"/> + <paramref name="data.Length"/>).
        ///
        /// A reply will only be received once the number of bytes specified
        /// in <see cref="ValidateFirmwareSize"/> have been written. That is,
        /// this command must be called multiple times to write the firmware,
        /// but the reply will only be received after the entire firmware file
        /// has been written.
        /// </remarks>
        public static Command<(byte checksum, uint count)> WriteFirmware(uint startAddress, ReadOnlySpan<byte> data, bool final)
        {
            if (data.Length > 14) {
                throw new ArgumentOutOfRangeException("data must be <= 14 bytes", nameof(data));
            }

            Memory<byte> message = new byte[data.Length + 6];
            message.Span[0] = 0x22;
            message.Span[1] = (byte)data.Length;
            BinaryPrimitives.WriteUInt32LittleEndian(message.Span.Slice(2, 4), startAddress);
            data.CopyTo(message.Slice(6).Span);

            var replyRequirement = final ? RequestTypes.Reply : RequestTypes.NoReply;
            return new Command<(byte checksum, uint endAddress)>(message, replyRequirement, reply => {
                var checksum = reply[1];
                var count = BinaryPrimitives.ReadUInt32LittleEndian(reply.Slice(1, 4));
                return (checksum, count);
            });
        }

        /// <summary>
        /// Creates a new reboot command.
        /// </summary>
        /// <returns>A command that should never receive a reply.</returns>
        /// <remarks>
        /// This command reboots the programmable brick. This command never
        /// receives a reply and will cause Bluetooth to disconnect as soon
        /// as it is sent.
        /// </remarks>
        public static Command<NoReply> Reboot()
        {
            var message = new byte[] { 0x33 };

            return new Command<NoReply>(message, RequestTypes.NoReply, reply => {
                throw new InvalidOperationException("Should never receive a reply for the reboot command");
            });
        }

        /// <summary>
        /// Creates a new firmware size validation command.
        /// </summary>
        /// <returns>A command that indicates if the size is valid or not.</returns>
        /// <remarks>
        /// This command is used to validate the size of a firmware file. The reply
        /// will return <see langword="true"/> if the size is OK, otherwise <see langword="false"/>;
        ///
        /// The <paramref name="size"/> is also used to determine when the <see cref="WriteFirmware"/>
        /// command will receive a reply.
        ///
        /// This command also has the size effect of resetting the checksum returned by
        /// <see cref="WriteFirmware"/> and <see cref="GetChecksum"/> to <c>0xFF</c>;
        /// </remarks>
        public static Command<bool> ValidateFirmwareSize(int size)
        {
            var message = new byte[] { 0x44 };

            return new Command<bool>(message, RequestTypes.Reply, reply => {
                throw new InvalidOperationException("Should never receive a reply for the reboot command");
            });
        }

        /// <summary>
        /// Creates a new command to get device info.
        /// </summary>
        /// <returns>A command with a replay that contains the firmware version, the starting address
        /// and the ending address</returns>
        /// <remarks>
        /// This command gets information about the programmable brick. <c>fwVersion</c> is the
        /// bootloader firmware version. <c>startAddress</c> is the starting address where the
        /// firmware should be written (i.e. this value is passed to <see cref="WriteFirmware"/>).
        /// <c>endAddess</c> is the final address where the firmware can be written. <c>hubType</c>
        /// indicates the type of programmable brick.
        /// </remarks>
        public static Command<(Version fwVersion, uint startAddress, uint endAddress, HubType hubType)> DeviceInfo()
        {
            var message = new byte[] { 0x55 };

            return new Command<(Version, uint, uint, HubType)>(message, RequestTypes.Reply, reply => {
                var fwVersion = VersionExtensions.ReadVersionLittleEndian(reply.Slice(1, 4));
                var startAddress = BinaryPrimitives.ReadUInt32LittleEndian(reply.Slice(5, 4));
                var endAddress = BinaryPrimitives.ReadUInt32LittleEndian(reply.Slice(9, 4));
                var hubType = (HubType)reply[13];
                return (fwVersion, startAddress, endAddress, hubType);
            });
        }

        /// <summary>
        /// Creates a new command to get the firmware checksum.
        /// </summary>
        /// <returns>A command whose reply contains the checksum.</returns>
        /// <remarks>
        /// This command does not actually compute the checksum, but just returns
        /// the current value.
        /// </remarks>
        public static Command<byte> GetChecksum()
        {
            var message = new byte[] { 0x66 };

            return new Command<byte>(message, RequestTypes.Reply, reply => {
                return reply[1];
            });
        }

        /// <summary>
        /// Creates a new command for getting the CPU protection level.
        /// </summary>
        /// <returns>A new command that returns the protection level.</returns>
        /// <remarks>
        /// This can be used to test if the debugger port on the programmable brick
        /// is useable. Not all devices support this command.
        /// </remarks>
        public static Command<FlashProtectionLevel> GetFlashProtectionLevel()
        {
            var message = new byte[] { 0x77 };

            return new Command<FlashProtectionLevel>(message, RequestTypes.Reply, reply => {
                return (FlashProtectionLevel)reply[1];
            });
        }


        /// <summary>
        /// Creates a new command that disconnects the Bluetooth connection.
        /// </summary>
        /// <returns>The command.</returns>
        /// <remarks>
        /// This command never has a reply. The Bluetooth connection will be
        /// closses as soon as this command is called.
        /// </remarks>
        public static Command<NoReply> Disconnect()
        {
            var message = new byte[] { 0x88 };

            return new Command<NoReply>(message, RequestTypes.NoReply, reply => {
                throw new InvalidOperationException("The disconnect command should never receive a reply");
            });
        }
    }
}
