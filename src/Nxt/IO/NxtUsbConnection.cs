using System;
using System.Threading.Tasks;
using Dandy.Devices.Usb;

namespace Dandy.Lms.Nxt.IO
{
    sealed class NxtUsbConnection : NxtConnection, IDisposable
    {
        private readonly Device device;

        internal NxtUsbConnection(Device device)
        {
            this.device = device ?? throw new System.ArgumentNullException(nameof(device));
        }

        public override void Dispose()
        {
            base.Dispose();
            device.Dispose();
        }

        protected override Task<ReadOnlyMemory<byte>> ReadAsync(int size)
        {
            throw new NotImplementedException();
        }

        protected override Task WriteAsync(ReadOnlyMemory<byte> data)
        {
            throw new NotImplementedException();
        }
    }
}
