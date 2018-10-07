using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dandy.Devices.BluetoothLE;
using Dandy.Lms.Internal;

namespace Dandy.Lms.PF2.FirmwareUpdate
{
    /// <summary>
    /// Class to manage a connection to a Powered Up programmable brick.
    /// </summary>
    public sealed class BLEConnection
    {
        /// <summary>
        /// The Bluetooth GATT service UUID for LEGO Powered Up hub firmware updater.
        /// </summary>
        internal static readonly Guid ServiceUuid = new Guid("00001625-1212-efde-1623-785feabcd123");

        /// <summary>
        /// The Bluetooth GATT charactaristic UUID for LEGO Powered Up hub firmware updater.
        /// </summary>
        internal static readonly Guid CharacteristicUuid = new Guid("00001626-1212-efde-1623-785feabcd123");

        private readonly Device device;
        private readonly GattCharacteristic characteristic;
        private readonly ConcurrentQueue<Memory<byte>> values;
        private readonly AutoResetEvent valueEvent;

        /// <summary>
        /// Gets the Blueooth address.
        /// </summary>
        public BluetoothAddress Address => device.BluetoothAddress;

        /// <summary>
        /// Gets the name of the Bluetooth device.
        /// </summary>
        public string Name => device.Name;

        BLEConnection(Device device, GattCharacteristic characteristic)
        {
            this.device = device ?? throw new ArgumentNullException(nameof(device));
            this.characteristic = characteristic ?? throw new ArgumentNullException(nameof(characteristic));
            characteristic.ValueChanged += Characteristic_ValueChanged;
            values = new ConcurrentQueue<Memory<byte>>();
            valueEvent = new AutoResetEvent(false);
        }

        private void Characteristic_ValueChanged(object sender, GattValueChangedEventArgs e)
        {
            values.Enqueue(e.Value);
            valueEvent.Set();
        }

        /// <summary>
        /// Creates a new instance of <see cref="BLEConnection"/> from a <see cref="Device"/>.
        /// </summary>
        /// <param name="device">The Bluetooth LE device.</param>
        /// <returns>The new connection.</returns>
        public static async Task<BLEConnection> FromDeviceAsync(Device device)
        {
            if (device == null) {
                throw new ArgumentNullException(nameof(device));
            }

            var services = await device.GetGattServicesAsync(ServiceUuid);
            var service = services.Single();
            var characteristics = await service.GetCharacteristicsAsync(CharacteristicUuid);
            var characteristic = characteristics.Single();
            var connection = new BLEConnection(device, characteristic);
            await characteristic.StartNotifyAsync();
            return connection;
        }

        /// <summary>
        /// Sends a command to the programmable brick and optionally awaits a reply.
        /// </summary>
        /// <typeparam name="T">The return parameter(s) of the reply.</typeparam>
        /// <param name="command">The command to send.</param>
        /// <param name="expectReply">If <see langword="true"/>, request and await a reply.</param>
        /// <returns>The reply value or <c>default(T)</c> if <paramref name="expectReply"/> was
        /// <see langword="false"/></returns>
        public async Task<T> SendCommandAsync<T>(Command<T> command, bool expectReply = true)
        {
            if (command == null) {
                throw new ArgumentNullException(nameof(command));
            }

            if (expectReply && command.ReplyRequirement == ReplyRequirement.Never) {
                throw new ArgumentException("This command cannot receive a reply", nameof(expectReply));
            }
            else if (!expectReply && command.ReplyRequirement == ReplyRequirement.Always) {
                throw new ArgumentException("This command must receive a reply", nameof(expectReply));
            }

            await characteristic.WriteValueAsync(command.Message, GattWriteOption.WriteWithoutResponse);
            if (expectReply) {
                await Task.Run(() => valueEvent.WaitOne());
                if (values.TryDequeue(out var reply)) {
                    return command.ParseReplay(reply.Span);
                }
            }
            return default;
        }
    }
}
