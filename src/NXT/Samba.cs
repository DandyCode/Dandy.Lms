using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Dandy.Devices.Serial;
using Dandy.Devices.USB.Libusb;

namespace Dandy.LMS.NXT
{
    public enum Error
    {
        Ok,
        NotPresent,
        Configuration,
        InUse,
        USBWrite,
        USBRead,
        SambaProtocol,
        Handshake,
        File,
        InvalidFirmware,
    }

    public sealed class ErrorException : Exception
    {
        public ErrorException(Error error) : this(error, null)
        {
        }

        public ErrorException(Error error, Exception innerException) : base(error.ToString(), innerException)
        {
            Error = error;
        }

        public Error Error { get; }
    }

    public sealed class Brick
    {
        const ushort idVendorLEGO = 0x0694;
        const ushort idProductNXT = 0x0002;

        const ushort idVendorAmtel = 0x03eb;
        const ushort idProductSamba = 0x6124;

        IDeviceInfo info;
        IDevice device;
        BinaryReader reader;
        BinaryWriter writer;
        bool isInResetMode;

        public async Task Find()
        {
            foreach (var info in await Factory.FindAllAsync()) {
                if (info.UsbVendorId == idVendorAmtel && info.UsbProductId == idProductSamba) {
                    this.info = info;
                    isInResetMode = true;
                    return;
                }
                if (info.UsbVendorId == idVendorLEGO && info.UsbProductId == idProductNXT) {
                    this.info = info;
                    return;
                }
            }

            throw new ErrorException(Error.NotPresent);
        }

        public async Task<object> Open()
        {
            device = await info.OpenAsync();
            if (device == null) {
                throw new ErrorException(Error.Configuration);
            }
            reader = new BinaryReader(device.InputStream, Encoding.ASCII);
            writer = new BinaryWriter(device.OutputStream, Encoding.ASCII);

            // NXT handshake
            SendStr("N#");
            var buf = RecvBuf(2);
            if (buf[0] != '\n' || buf[1] != '\r') {
                device.Dispose();
                device = null;
                throw new ErrorException(Error.Handshake);
            }

            return null;
        }

        public void Close()
        {
            reader?.Dispose();
            writer?.Dispose();
            device?.Dispose();
            device = null;
        }

        public bool IsInResetMode => isInResetMode;

        void SendBuf(byte[] buf)
        {
            writer.Write(buf, 0, buf.Length);
            writer.Flush();
        }

        void SendStr(string str)
        {
            writer.Write(str);
            writer.Flush();
        }

        byte[] RecvBuf(int len)
        {
            var buf = reader.ReadBytes(len);
            return buf;
        }

        static string FormatCommand(char cmd, uint addr, uint word)
        {
            return string.Format("{0}{1:X8},{2:X8}#", cmd, addr, word);
        }

        static string FormatCommand(char cmd, uint addr)
        {
            return string.Format("{0}{1:X8}#", cmd, addr);
        }

        void WriteCommon(char type, uint addr, uint w)
        {
            var cmd = FormatCommand(type, addr, w);
            SendStr(cmd);
        }

        public void WriteByte(uint addr, byte b)
        {
            WriteCommon('O', addr, b);
        }

        public void WriteHWord(uint addr, ushort hw)
        {
            WriteCommon('H', addr, hw);
        }

        public void WriteWord(uint addr, uint w)
        {
            WriteCommon('W', addr, w);
        }

        byte[] ReadCommon(char cmd, int len, uint addr)
        {
            var c = FormatCommand(cmd, addr, (uint)len);
            SendStr(c);
            var buf = RecvBuf(len);
            // TODO: convert from little-endian to CPU-endian
            return buf;
        }

        public byte ReadByte(uint addr)
        {
            var buf = ReadCommon('o', 1, addr);
            return buf[0];
        }

        public ushort ReadHWord(uint addr)
        {
            var buf = ReadCommon('h', 2, addr);
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(buf);
            return BitConverter.ToUInt16(buf, 0);
        }

        public uint ReadWord(uint addr)
        {
            var buf = ReadCommon('w', 4, addr);
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(buf);
            return BitConverter.ToUInt32(buf, 0);
        }

        public void SendFile(uint addr, byte[] file)
        {
            var cmd = FormatCommand('S', addr, (uint)file.Length);
            SendStr(cmd);
            SendBuf(file);
        }

        public byte[] RecvFile(uint addr, int len)
        {
            var cmd = FormatCommand('R', addr, (uint)len);
            SendStr(cmd);
            return RecvBuf(len + 1);
        }

        public void Jump(uint addr)
        {
            var cmd = FormatCommand('G', addr);
            SendStr(cmd);
        }

        public Version SambaVersion()
        {
            SendStr("V#");
            var buf = RecvBuf(4);
            var str = Encoding.ASCII.GetString(buf);
            return new Version(str);
        }
    }
}
