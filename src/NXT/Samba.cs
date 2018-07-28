using System;
using System.Text;

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
        public ErrorException(Error error) : base(error.ToString())
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

        Context ctx;
        Device dev;
        DeviceHandle hdl;
        bool isInResetMode;

        public Brick()
        {
            ctx = new Context();
        }

        public void Find()
        {
            foreach (var dev in new DeviceList()) {
                if (dev.Descriptor.idVendor == idVendorAmtel && dev.Descriptor.idProduct == idProductSamba) {
                    this.dev = dev;
                    isInResetMode = true;
                    return;
                }
                if (dev.Descriptor.idVendor == idVendorLEGO && dev.Descriptor.idProduct == idProductNXT) {
                    this.dev = dev;
                    return;
                }
            }

            throw new ErrorException(Error.NotPresent);
        }

        public void Open()
        {
            hdl = dev.Open();
            try {
                if (hdl.Configuration == 1) {
                    if (hdl.IsKernelDriverActive(1)) {
                        hdl.DetachKernelDriver(1);
                    }
                }
                else {
                    hdl.Configuration = 1;
                }
            }
            catch (ErrorException) {
                hdl.Dispose();
                hdl = null;
                throw new ErrorException(Error.Configuration);
            }

            try {
                hdl.ClaimInterface(1);
            }
            catch (ErrorException) {
                hdl.Dispose();
                hdl = null;
                throw new ErrorException(Error.InUse);
            }

            // NXT handshake
            SendStr("N#");
            var buf = RecvBuf(2);
            if (buf[0] != '\n' || buf[1] != '\r') {
                hdl.ReleaseInterface(1);
                hdl.Dispose();
                hdl = null;
                throw new ErrorException(Error.Handshake);
            }
        }

        public void Close()
        {
            hdl?.ReleaseInterface(1);
            hdl?.Dispose();
            hdl = null;
        }

        public bool IsInResetMode => isInResetMode;

        void SendBuf(byte[] buf)
        {
            int ret = hdl.BulkTransfer(0x1, buf);
            if (ret < 0) {
                throw new ErrorException(Error.USBWrite);
            }
        }

        void SendStr(string str)
        {
            var asc = Encoding.ASCII.GetBytes(str);
            SendBuf(asc);
        }

        byte[] RecvBuf(int len)
        {
            var buf = new byte[len];
            var ret = hdl.BulkTransfer(0x82, buf);
            if (ret < 0) {
                throw new ErrorException(Error.USBRead);
            }
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
