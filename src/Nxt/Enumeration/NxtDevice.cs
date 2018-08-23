using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dandy.Devices.Usb;
using Dandy.Lms.Nxt.IO;

namespace Dandy.Lms.Nxt.Enumeration
{
    public sealed class NxtDevice
    {
        const ushort idVendorLego = 0x0694;
        const ushort idProductNxt = 0x0002;
        private readonly DeviceInfo usbInfo;

        public static async Task<IEnumerable<NxtDevice>> FindAllUsbAsync()
        {
            var usbDevices = await Factory.FindAllAsync(idVendorLego, idProductNxt);
            return usbDevices.Select(d => new NxtDevice(d));
        }

        NxtDevice(DeviceInfo usbInfo)
        {
            this.usbInfo = usbInfo;
        }

        public async Task<NxtConnection> ConnectAsync(ConnectionType type)
        {
            switch (type) {
                case ConnectionType.Bluetooth:
                    throw new NotImplementedException();
                case ConnectionType.Usb:
                    var device = await usbInfo.OpenAsync();
                    return new NxtUsbConnection(device);
                default:
                    throw new ArgumentException("Invalid enum value", nameof(type));
            }
        }

        public Task<NxtConnection> ConnectAsync() => ConnectAsync(ConnectionType.Usb); // TODO: select Bluetooth if USB is not available
    }
}
