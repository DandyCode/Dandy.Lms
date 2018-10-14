using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;
using Dandy.Devices.BluetoothLE;
using System.Linq;

namespace Dandy.Lms.PF2
{
    /// <summary>
    /// Class that monitors for new Powered Up Smart Hub connections
    /// </summary>
    public sealed class HubWatcher
    {
        private readonly ILogger logger;
        private readonly AdvertisementWatcher watcher;
        private readonly List<BluetoothAddress> pendingAddresses;

        /// <summary>
        /// Event that is fired when a new hub is connected.
        /// </summary>
        public event EventHandler<Hub> HubConnected;

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param name="logger">Optional logger for debug messages.</param>
        public HubWatcher(ILogger logger = null)
        {
            this.logger = logger;
            pendingAddresses = new List<BluetoothAddress>();
            watcher = new AdvertisementWatcher(BLEConnection.ServiceUuid);
            watcher.Received += Watcher_Received;
        }

        /// <summary>
        /// Start scanning for Bluetooth devices.
        /// </summary>
        public void Start() => watcher.Start();

        /// <summary>
        /// Stop scanning for Bluetooth devices.
        /// </summary>
        public void Stop() => watcher.Stop();

        private async void Watcher_Received(object sender, AdvertisementReceivedEventArgs e)
        {
            try {
                if (pendingAddresses.Contains(e.Address)) {
                    return;
                }
                pendingAddresses.Add(e.Address);
                var device = await Device.FromAddressAsync(e.Address);
                var connection = await BLEConnection.FromDeviceAsync(device);
                var hub = new Hub(connection);
                HubConnected?.Invoke(this, hub);
            }
            catch (Exception ex) {
                logger?.LogDebug(ex, "Unhandled exception in Watcher_Received");
            }
            pendingAddresses.Remove(e.Address);
        }
    }
}
