using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dandy.Devices.Serial;

namespace Dandy.Lms.Nxt
{
    public sealed class Samba
    {
        const ushort idVendorAmtel = 0x03eb;
        const ushort idProductSamba = 0x6124;

        DeviceInfo info;
        Device device;

        public static async Task<IEnumerable<Samba>> FindAllAsync()
        {
            var factory = Factory.GetFactoryForOSPlatform();
            var devices = await factory.FindAllAsync(idVendorAmtel, idProductSamba);
            return devices.Select(d => new Samba(d));
        }

        Samba(DeviceInfo info)
        {
            this.info = info;
        }

        public async Task<IDisposable> OpenAsync()
        {
            device = await info.OpenAsync();
            if (device.InputStream.CanTimeout) {
                device.InputStream.ReadTimeout = 500;
            }
            if (device.OutputStream.CanTimeout) {
                device.OutputStream.WriteTimeout = 500;
            }
            device.BaudRate = 115200;

            // NXT handshake
            await SetNormalModeAsync();
            var buf = await ReadBytesAsync(2);
            if (buf[0] != '\n' || buf[1] != '\r') {
                device.Dispose();
                device = null;
                throw new IOException("Did not respond to handshake");
            }

            return device;
        }

        async Task WriteBytesAsync(byte[] buf)
        {
            await device.OutputStream.WriteAsync(buf, 0, buf.Length);
            await device.OutputStream.FlushAsync();
        }

        async Task WriteStringAsync(string str)
        {
            var buf = Encoding.ASCII.GetBytes(str);
            await WriteBytesAsync(buf);
        }

        async Task<byte[]> ReadBytesAsync(int len)
        {
            var buf = new byte[len];
            var bytesRead = 0;
            while (bytesRead < len) {
                bytesRead += await device.InputStream.ReadAsync(buf, bytesRead, len - bytesRead);
            }
            return buf;
        }

        static string FormatCommand(char cmd, uint addr, uint w)
        {
            return string.Format("{0}{1:X8},{2:X8}#", cmd, addr, w);
        }

        static string FormatCommand(char cmd, uint addr)
        {
            return string.Format("{0}{1:X8}#", cmd, addr);
        }

        public Task SetNormalModeAsync()
        {
            return WriteStringAsync("N#");
        }

        public Task SetTerminalModeAsync()
        {
            return WriteStringAsync("T#");
        }

        public Task WriteByteAsync(uint addr, byte b)
        {
            var cmd = string.Format("O{0:X8},{1:X2}#", addr, b);
            return WriteStringAsync(cmd);
        }

        public Task WriteHalfWordAsync(uint addr, ushort hw)
        {
            var cmd = string.Format("H{0:X8},{1:X4}#", addr, hw);
            return WriteStringAsync(cmd);
        }

        public Task WriteWordAsync(uint addr, uint w)
        {
            var cmd = string.Format("W{0:X8},{1:X8}#", addr, w);
            return WriteStringAsync(cmd);
        }

        async Task<byte[]> ReadAsync(char cmd, int len, uint addr)
        {
            var c = FormatCommand(cmd, addr, (uint)len);
            await WriteStringAsync(c);
            var buf = await ReadBytesAsync(len);
            return buf;
        }

        public async Task<byte> ReadByteAysnc(uint addr)
        {
            var buf = await ReadAsync('o', 1, addr);
            return buf[0];
        }

        public async Task<ushort> ReadHalfWordAsync(uint addr)
        {
            var buf = await ReadAsync('h', 2, addr);
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(buf);
            return BitConverter.ToUInt16(buf, 0);
        }

        public async Task <uint> ReadWordAsync(uint addr)
        {
            var buf = await ReadAsync('w', 4, addr);
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(buf);
            return BitConverter.ToUInt32(buf, 0);
        }

        public async Task SendFileAsync(uint addr, byte[] file)
        {
            var cmd = FormatCommand('S', addr, (uint)file.Length);
            await WriteStringAsync(cmd);
            await WriteBytesAsync(file);
        }

        public async Task<byte[]> ReceiveFileAsync(uint addr, int len)
        {
            var cmd = FormatCommand('R', addr, (uint)len);
            await WriteStringAsync(cmd);
            var buf = await ReadBytesAsync(len + 1);
            return buf;
        }

        public Task GoAsync(uint addr)
        {
            var cmd = string.Format("G{0:X8}#", addr);
            return WriteStringAsync(cmd);
        }

        public async Task<Version> DisplayVersionAsync()
        {
            await WriteStringAsync("V#");
            var buf = await ReadBytesAsync(4);
            var str = Encoding.ASCII.GetString(buf);
            return new Version(str);
        }
    }
}
