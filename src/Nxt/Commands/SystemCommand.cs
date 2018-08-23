
using System;
using System.Linq;
using System.Text;
using Dandy.Devices.Bluetooth;

namespace Dandy.Lms.Nxt.Commands
{
    public static class SystemCommand
    {
        const int maxPayloadSize = 64;

        static void EncodeCommandType(this Memory<byte> mem, CommandType type)
        {
            mem.Span[0] = (byte)type;
        }

        static void EncodeDirectCommand(this Memory<byte> mem, SystemCommandCode code)
        {
            mem.Span[0] = (byte)code;
        }

        static void EncodeByte(this Memory<byte> mem, byte value)
        {
            mem.Span[0] = value;
        }

        static void EncodeInt16(this Memory<byte> mem, short value)
        {
            mem.Span[0] = unchecked((byte)value);
            mem.Span[1] = unchecked((byte)(value >> 8));
        }

        static void EncodeInt32(this Memory<byte> mem, int value)
        {
            mem.Span[0] = unchecked((byte)value);
            mem.Span[1] = unchecked((byte)(value >> 8));
            mem.Span[2] = unchecked((byte)(value >> 16));
            mem.Span[3] = unchecked((byte)(value >> 24));
        }

        static void EncodeString(this Memory<byte> mem, string str, int maxSize)
        {
            if (str == null) {
                throw new ArgumentNullException(nameof(str));
            }

            var bytes = Encoding.ASCII.GetBytes(str);

            if (bytes.Length < 1 || bytes.Length > maxSize) {
                throw new ArgumentException($"String must be between 1 and {maxSize} characters", nameof(str));
            }

            bytes.AsMemory().CopyTo(mem);
        }

        static int DecodeInt16(this ReadOnlySpan<byte> span)
        {
            return span[0] + span[1] << 8;
        }

        static int DecodeInt32(this ReadOnlySpan<byte> span)
        {
            return span[0] + span[1] << 8 + span[2] << 16 + span[3] << 24;
        }

        static string DecodeString(this ReadOnlySpan<byte> span)
        {
            return Encoding.ASCII.GetString(span.Slice(0, span.IndexOf<byte>(0)).ToArray());
        }

        /// <summary>
        /// Opens a file for reading.
        /// </summary>
        /// <remarks>
        /// A close command is required to close the returned handle when it is
        /// no longer needed.
        /// </remarks>
        /// <param name="filename">The name of the file. Must be 1 - 19 characters.</param>
        public static Command<(byte handle, int size)> OpenRead(string filename)
        {
            var payload = new byte[22].AsMemory();
            payload.Slice(0, 1).EncodeCommandType(CommandType.Direct);
            payload.Slice(1, 1).EncodeDirectCommand(SystemCommandCode.OpenRead);
            payload.Slice(2, 20).EncodeString(filename, 19);
            return new Command<(byte, int)>(payload, 8, p => (p[3], p.Slice(4, 4).DecodeInt32()));
        }

        /// <summary>
        /// Opens a file for writing.
        /// </summary>
        /// <remarks>
        /// A close command is required to close the returned handle when it is
        /// no longer needed.
        /// </remarks>
        /// <param name="filename">The name of the file. Must be 1 - 19 characters.</param>
        /// <param name="size">The size of the file.</param>
        public static Command<byte> OpenWrite(string filename, int size)
        {
            var payload = new byte[26].AsMemory();
            payload.Slice(0, 1).EncodeCommandType(CommandType.Direct);
            payload.Slice(1, 1).EncodeDirectCommand(SystemCommandCode.OpenWrite);
            payload.Slice(2, 20).EncodeString(filename, 19);
            payload.Slice(22, 4).EncodeInt32(size);
            return new Command<byte>(payload, 4, p => p[3]);
        }

        /// <summary>
        /// Continuation of the <see cref="OpenRead"/> command.
        /// </summary>
        /// <param name="handle">The file handle returned by an <see cref="OpenRead"/> command.</param>
        /// <param name="size">The number of bytes to read. Max size is 58 bytes.</param>
        public static Command<(byte handle, ReadOnlyMemory<byte> data)> Read(byte handle, int size)
        {
            if (size < 0 || size > maxPayloadSize - 6) {
                throw new ArgumentOutOfRangeException(nameof(size));
            }

            var payload = new byte[5].AsMemory();
            payload.Slice(0, 1).EncodeCommandType(CommandType.Direct);
            payload.Slice(1, 1).EncodeDirectCommand(SystemCommandCode.Read);
            payload.Slice(2, 1).EncodeByte(handle);
            payload.Slice(3, 2).EncodeInt16((short)size);
            return new Command<(byte, ReadOnlyMemory<byte>)>(payload, 6 + size, p => {
                var replyHandle = p[3];
                var replySize = p.Slice(4, 2).DecodeInt16();
                var replyData = p.Slice(6, replySize);
                return (replyHandle, replyData.ToArray());
            });
        }

        /// <summary>
        /// Continuation of the <see cref="OpenWrite"/> command.
        /// </summary>
        /// <param name="handle">The file handle returned by an <see cref="OpenWrite"/> command.</param>
        /// <param name="data">The data to write. Max size is 61 bytes.</param>
        public static Command<(byte handle, int size)> Write(byte handle, ReadOnlySpan<byte> data)
        {
            if (data.Length > maxPayloadSize - 3) {
                throw new ArgumentException("data is too big", nameof(data));
            }
            var payload = new byte[3 + data.Length].AsMemory();
            payload.Slice(0, 1).EncodeCommandType(CommandType.Direct);
            payload.Slice(1, 1).EncodeDirectCommand(SystemCommandCode.Write);
            payload.Slice(2, 1).EncodeByte(handle);
            data.CopyTo(payload.Slice(3).Span);
            return new Command<(byte, int)>(payload, 6, p => (p[3], p.Slice(4, 2).DecodeInt16()));
        }

        /// <summary>
        /// Closes a file handle.
        /// </summary>
        /// <param name="handle">The file handle returned by an <see cref="OpenRead"/>
        /// or <see cref="OpenWrite"/> command.</param>
        public static Command<byte> Close(byte handle)
        {
            var payload = new byte[3].AsMemory();
            payload.Slice(0, 1).EncodeCommandType(CommandType.Direct);
            payload.Slice(1, 1).EncodeDirectCommand(SystemCommandCode.Close);
            payload.Slice(2, 1).EncodeByte(handle);
            return new Command<byte>(payload, 4, p => p[3]);
        }

        /// <summary>
        /// Deletes a file.
        /// </summary>
        /// <param name="filename">The name of the file. Must be 1 - 19 characters.</param>
        public static Command<string> Delete(string filename)
        {
            var payload = new byte[22].AsMemory();
            payload.Slice(0, 1).EncodeCommandType(CommandType.Direct);
            payload.Slice(1, 1).EncodeDirectCommand(SystemCommandCode.Delete);
            payload.Slice(2, 20).EncodeString(filename, 19);
            return new Command<string>(payload, 23, p => p.Slice(3, 20).DecodeString());
        }

        /// <summary>
        /// Finds a file.
        /// </summary>
        /// <param name="filename">The name of the file and/or file extension (wildcards allowed).
        /// Must be 1 - 19 characters.</param>
        public static Command<(byte handle, string filename, int size)> FindFirst(string filename)
        {
            var payload = new byte[22].AsMemory();
            payload.Slice(0, 1).EncodeCommandType(CommandType.Direct);
            payload.Slice(1, 1).EncodeDirectCommand(SystemCommandCode.FindFirst);
            payload.Slice(2, 20).EncodeString(filename, 19);
            return new Command<(byte, string, int)>(payload, 28, p => {
                var replyHandle = p[3];
                var replyFileName = p.Slice(4, 20).DecodeString();
                var replySize = p.Slice(24, 4).DecodeInt32();
                return (replyHandle, replyFileName, replySize);
            });
        }

        /// <summary>
        /// Continuation of the <see cref="FindFirst"/> command.
        /// </summary>
        /// <param name="handle">Handle returned by <see cref="FindFirst"/> or
        /// the previous <see cref="FindNext"/> command.</param>
        public static Command<(byte handle, string filename, int size)> FindNext(string filename)
        {
            var payload = new byte[3].AsMemory();
            payload.Slice(0, 1).EncodeCommandType(CommandType.Direct);
            payload.Slice(1, 1).EncodeDirectCommand(SystemCommandCode.FindNext);
            return new Command<(byte, string, int)>(payload, 28, p => {
                var replyHandle = p[3];
                var replyFileName = p.Slice(4, 20).DecodeString();
                var replySize = p.Slice(24, 4).DecodeInt32();
                return (replyHandle, replyFileName, replySize);
            });
        }

        /// <summary>
        /// Gets the firmware version.
        /// </summary>
        public static Command<(Version protocol, Version firmware)> GetFirmwareVersion()
        {
            var payload = new byte[3].AsMemory();
            payload.Slice(0, 1).EncodeCommandType(CommandType.Direct);
            payload.Slice(1, 1).EncodeDirectCommand(SystemCommandCode.GetFirmwareVersion);
            return new Command<(Version, Version)>(payload, 7, p => {
                var protocolVersion = new Version(p[4], p[3]);
                var firmwareVersion = new Version(p[6], p[5]);
                return (protocolVersion, firmwareVersion);
            });
        }

        /// <summary>
        /// Opens a file for writing.
        /// </summary>
        /// <remarks>
        /// A close command is required to close the returned handle when it is
        /// no longer needed.
        /// </remarks>
        /// <param name="filename">The name of the file. Must be 1 - 19 characters.</param>
        /// <param name="size">The size of the file.</param>
        public static Command<byte> OpenWriteLinear(string filename, int size)
        {
            var payload = new byte[26].AsMemory();
            payload.Slice(0, 1).EncodeCommandType(CommandType.Direct);
            payload.Slice(1, 1).EncodeDirectCommand(SystemCommandCode.OpenWriteLinear);
            payload.Slice(2, 20).EncodeString(filename, 19);
            payload.Slice(22, 4).EncodeInt32(size);
            return new Command<byte>(payload, 4, p => p[3]);
        }

        /// <summary>
        /// Opens a file for reading.
        /// </summary>
        /// <param name="filename">The name of the file. Must be 1 - 19 characters.</param>
        public static Command<int> OpenReadLinear(string filename)
        {
            var payload = new byte[22].AsMemory();
            payload.Slice(0, 1).EncodeCommandType(CommandType.Direct);
            payload.Slice(1, 1).EncodeDirectCommand(SystemCommandCode.OpenRead);
            payload.Slice(2, 20).EncodeString(filename, 19);
            return new Command<int>(payload, 7, p => p.Slice(3, 4).DecodeInt32());
        }

        /// <summary>
        /// Opens a file for writing.
        /// </summary>
        /// <remarks>
        /// A close command is required to close the returned handle when it is
        /// no longer needed.
        /// </remarks>
        /// <param name="filename">The name of the file. Must be 1 - 19 characters.</param>
        /// <param name="size">The size of the file.</param>
        public static Command<byte> OpenWriteData(string filename, int size)
        {
            var payload = new byte[26].AsMemory();
            payload.Slice(0, 1).EncodeCommandType(CommandType.Direct);
            payload.Slice(1, 1).EncodeDirectCommand(SystemCommandCode.OpenWriteData);
            payload.Slice(2, 20).EncodeString(filename, 19);
            payload.Slice(22, 4).EncodeInt32(size);
            return new Command<byte>(payload, 4, p => p[3]);
        }

        /// <summary>
        /// Opens a file for appending.
        /// </summary>
        /// <remarks>
        /// A close command is required to close the returned handle when it is
        /// no longer needed.
        /// </remarks>
        /// <param name="filename">The name of the file. Must be 1 - 19 characters.</param>
        public static Command<(byte handle, int size)> OpenAppendData(string filename)
        {
            var payload = new byte[22].AsMemory();
            payload.Slice(0, 1).EncodeCommandType(CommandType.Direct);
            payload.Slice(1, 1).EncodeDirectCommand(SystemCommandCode.OpenAppendData);
            payload.Slice(2, 20).EncodeString(filename, 19);
            return new Command<(byte, int)>(payload, 8, p => (p[3], p.Slice(4, 4).DecodeInt32()));
        }

        /// <summary>
        /// Boots into firmware update mode. Only valid on USB connection.
        /// </summary>
        public static Command<bool> Boot()
        {
            var payload = new byte[21].AsMemory();
            payload.Slice(0, 1).EncodeCommandType(CommandType.Direct);
            payload.Slice(1, 1).EncodeDirectCommand(SystemCommandCode.Boot);
            payload.Slice(2, 19).EncodeString("Let's dance: SAMBA", 18);
            return new Command<bool>(payload, 7, p => p.Slice(3, 4).DecodeString() == "Yes");
        }

        /// <summary>
        /// Sets the brick name. Must be 1 - 15 characters.
        /// </summary>
        public static Command<object> SetBrickName(string name)
        {
            var payload = new byte[21].AsMemory();
            payload.Slice(0, 1).EncodeCommandType(CommandType.Direct);
            payload.Slice(1, 1).EncodeDirectCommand(SystemCommandCode.SetBrickName);
            payload.Slice(2, 16).EncodeString(name, 15);
            return new Command<object>(payload, 3, p => null);
        }

        /// <summary>
        /// Gets brick name, Bluetooth address, Bluetooth signal strength and free space
        /// </summary>
        public static Command<(string brickName, BluetoothAddress address, int signal, int free)> GetDeviceInfo()
        {
            var payload = new byte[2].AsMemory();
            payload.Slice(0, 1).EncodeCommandType(CommandType.Direct);
            payload.Slice(1, 1).EncodeDirectCommand(SystemCommandCode.GetDeviceInfo);
            return new Command<(string, BluetoothAddress, int, int)>(payload, 33, p => {
                var replyBrickName = p.Slice(3, 15).DecodeString();
                var replyAddress = BluetoothAddress.FromSpan(p.Slice(18, 6));
                var replySignal = p.Slice(25, 4).DecodeInt32();
                var replyFree = p.Slice(29, 4).DecodeInt32();
                return (replyBrickName, replyAddress, replySignal, replyFree);
            });
        }
    }
}
