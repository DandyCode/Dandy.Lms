using System;
using System.Threading.Tasks;

namespace Dandy.Lms.Nxt.IO
{
    sealed class NxtBluetooth : NxtConnection
    {
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
