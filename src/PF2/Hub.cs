using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Dandy.Devices.BluetoothLE;

using static Dandy.Lms.PF2.CommandFactory;

namespace Dandy.Lms.PF2
{
    public sealed class Hub
    {
        private readonly BLEConnection connection;

        internal Hub(BLEConnection connection)
        {
            this.connection = connection ?? throw new ArgumentNullException(nameof(connection));
        }

        public Task<string> GetNameAsync()
        {
            return connection.SendCommandAsync(GetName());
        }

        public Task SetNameAsync(string name)
        {
            return connection.SendCommandNoReplyAsync(SetName(name));
        }

        public Task<IDisposable> SubscribeNameAsync(IObserver<string> observer)
        {
            return connection.SubscribeCommandAsync(SubscribeName(), UnsubscribeName(), observer);
        }

        public Task<Version> GetFirmwareVersionAsync()
        {
            return connection.SendCommandAsync(GetFirmwareVersion());
        }
        public Task<Version> GeHardwareVersionAsync()
        {
            return connection.SendCommandAsync(GetHardwareVersion());
        }
    }
}
