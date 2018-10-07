using System;
using System.Collections.Generic;
using System.Text;
using Dandy.Devices.BluetoothLE;
using Microsoft.Extensions.Logging;

namespace Dandy.Lms.PF2.FirmwareUpdate
{
    /// <summary>
    /// Class used to scan for Powered Up hubs.
    /// </summary>
    public sealed class HubWatcher
    {
        private readonly ReadOnlyMemory<byte> manfuacturerData;
        private readonly AdvertisementWatcher watcher;
        private readonly ILogger logger;
        private readonly Dictionary<BluetoothAddress, AdvertisementReceivedEventArgs> ads;

        /// <summary>
        /// Event that fires when a hub has been connected.
        /// </summary>
        public event EventHandler<Hub> HubConnected;

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param name="hubType">The type of hub to match.</param>
        /// <param name="logger">Optional logger for debugging.</param>
        public HubWatcher(HubType hubType, ILogger logger = null)
        {
            if (!Enum.IsDefined(typeof(HubType), hubType)) {
                throw new ArgumentOutOfRangeException(nameof(hubType));
            }
            manfuacturerData = new byte[] { 0x00, 0x00, 0x00, 0x10, (byte)hubType, 0x02 };
            watcher = new AdvertisementWatcher(BLEConnection.ServiceUuid);
            watcher.Received += Watcher_Received;
            this.logger = logger;
            ads = new Dictionary<BluetoothAddress, AdvertisementReceivedEventArgs>();
        }

        private async void Watcher_Received(object sender, AdvertisementReceivedEventArgs e)
        {
            try {
                // make sure this is the right kind of hub
                if (!e.Advertisement.ManufacturerData[0x0397].Span.SequenceEqual(manfuacturerData.Span)) {
                    return;
                }

                // ignore duplicate adverts
                if (ads.ContainsKey(e.Address)) {
                    ads[e.Address] = e;
                    return;
                }
                
                var device = await Device.FromAddressAsync(e.Address);
                var connection = await BLEConnection.FromDeviceAsync(device);
                var hub = new Hub(connection);
                HubConnected?.Invoke(this, hub);
            }
            catch (Exception ex) {
                logger?.LogDebug(ex, "Unhandled exception in HubWatcher.Watcher_Received");
            }
        }

        /// <summary>
        /// Start scanning for Bluetooth devices.
        /// </summary>
        public void StartScan()
        {
            watcher.Start();
        }

        /// <summary>
        /// Stop scanning for Bluetooth devices.
        /// </summary>
        public void StopScan()
        {
            watcher.Stop();
        }
    }
}
