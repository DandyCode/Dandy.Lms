using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Dandy.Devices.BluetoothLE;

using static Dandy.Lms.PF2.CommandFactory;

namespace Dandy.Lms.PF2
{
    /// <summary>
    /// Represents a LEGO Powered Up hub. New intances are obtained via <see cref="HubWatcher"/>.
    /// </summary>
    public sealed class Hub
    {
        private readonly BLEConnection connection;

        internal Hub(BLEConnection connection)
        {
            this.connection = connection ?? throw new ArgumentNullException(nameof(connection));
        }

        /// <summary>
        /// Sends a message to the hub to get the current name.
        /// </summary>
        /// <returns>A task that awaits the reply from the hub with
        /// the name.</returns>
        /// <seealso cref="SubscribeNameAsync"/>
        public Task<string> GetNameAsync()
        {
            return connection.SendCommandAsync(GetName());
        }

        /// <summary>
        /// Sends a message to the hub to change the name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns>A task that completes when the message has been sent.</returns>
        /// <seealso cref="ResetNameAsync"/>
        public Task SetNameAsync(string name)
        {
            return connection.SendCommandNoReplyAsync(SetName(name));
        }

        /// <summary>
        /// Sends a message to the hub to reset the name back to the default value.
        /// </summary>
        /// <returns>A task that completes when the message has been sent.</returns>
        /// <seealso cref="SetNameAsync(string)"/>
        public Task ResetNameAsync()
        {
            return connection.SendCommandNoReplyAsync(ResetName());
        }

        /// <summary>
        /// Sends a message to the hub to subscribe to name changes.
        /// </summary>
        /// <param name="observer">Receives change notifications.</param>
        /// <returns>A task with a <see cref="IDisposable"/> for unsubscribing.</returns>
        /// <remarks>The task returns once the subscribe message has been sent.
        /// The observer will receive <see cref="IObserver{T}.OnNext(T)"/> notifications
        /// until the returned <see cref="IDisposable"/> is disposed or there is
        /// a communication error. <see cref="IObserver{T}.OnCompleted()"/> will
        /// be called if the unsubscribe command is sucessfully sent (when
        /// <see cref="IDisposable.Dispose()"/> is called) or
        /// <see cref="IObserver{T}.OnError(Exception)"/> is called if there is an error.
        /// </remarks>
        /// <seealso cref="GetNameAsync"/>
        public Task<IDisposable> SubscribeNameAsync(IObserver<string> observer)
        {
            return connection.SubscribeCommandAsync(SubscribeName(), UnsubscribeName(), observer);
        }

        /// <summary>
        /// Sends a message to the hub to get the current button state.
        /// </summary>
        /// <returns>A task that awaits the reply from the hub with the
        /// button state. (<see langword="true"/> means the button is pressed.)
        /// </returns>
        /// <seealso cref="SubscribeButtonStateAsync"/>
        public Task<bool> GetButtonStateAsync()
        {
            return connection.SendCommandAsync(GetButtonState());
        }

        /// <summary>
        /// Sends a message to the hub to subscribe to button state changes.
        /// </summary>
        /// <param name="observer">Receives change notifications.</param>
        /// <returns>A task with a <see cref="IDisposable"/> for unsubscribing.</returns>
        /// <remarks>The task returns once the subscribe message has been sent.
        /// The observer will receive <see cref="IObserver{T}.OnNext(T)"/> notifications
        /// until the returned <see cref="IDisposable"/> is disposed or there is
        /// a communication error. <see cref="IObserver{T}.OnCompleted()"/> will
        /// be called if the unsubscribe command is sucessfully sent (when
        /// <see cref="IDisposable.Dispose()"/> is called) or
        /// <see cref="IObserver{T}.OnError(Exception)"/> is called if there is an error.
        /// </remarks>
        /// <seealso cref="GetButtonStateAsync"/>
        public Task<IDisposable> SubscribeButtonStateAsync(IObserver<bool> observer)
        {
            return connection.SubscribeCommandAsync(SubscribeButtonState(), UnsubscribeButtonState(), observer);
        }

        /// <summary>
        /// Sends a command to the hub to get the firmware version.
        /// </summary>
        /// <returns>A task that awaits a reply from the hub containing
        /// the firmware version.</returns>
        public Task<Version> GetFirmwareVersionAsync()
        {
            return connection.SendCommandAsync(GetFirmwareVersion());
        }

        /// <summary>
        /// Sends a command to the hub to get the hardware version.
        /// </summary>
        /// <returns>A task that awaits a reply from the hub containing
        /// the hardware version.</returns>
        public Task<Version> GetHardwareVersionAsync()
        {
            return connection.SendCommandAsync(GetHardwareVersion());
        }

        /// <summary>
        /// Sends a command to the hub to get the received signal strength indication.
        /// </summary>
        /// <returns>A task that awaits a reply from the hub containing
        /// the received signal strength indication in dBm.</returns>
        /// <seealso cref="SubscribeRSSIAsync"/>
        public Task<sbyte> GetRSSIAsync()
        {
            return connection.SendCommandAsync(GetRSSI());
        }

        /// <summary>
        /// Sends a message to the hub to subscribe to received signal strength indication changes.
        /// </summary>
        /// <param name="observer">Receives change notifications.</param>
        /// <returns>A task with a <see cref="IDisposable"/> for unsubscribing.</returns>
        /// <remarks>The task returns once the subscribe message has been sent.
        /// The observer will receive <see cref="IObserver{T}.OnNext(T)"/> notifications
        /// until the returned <see cref="IDisposable"/> is disposed or there is
        /// a communication error. <see cref="IObserver{T}.OnCompleted()"/> will
        /// be called if the unsubscribe command is sucessfully sent (when
        /// <see cref="IDisposable.Dispose()"/> is called) or
        /// <see cref="IObserver{T}.OnError(Exception)"/> is called if there is an error.
        /// </remarks>
        /// <seealso cref="GetRSSIAsync"/>
        public Task<IDisposable> SubscribeRSSIAsync(IObserver<sbyte> observer)
        {
            return connection.SubscribeCommandAsync(SubscribeRSSI(), UnsubscribeRSSI(), observer);
        }

        /// <summary>
        /// Sends a command to the hub to get the battery level (0 to 100%).
        /// </summary>
        /// <returns>A task that awaits a reply from the hub containing
        /// the battery level in percent full.</returns>
        /// <seealso cref="SubscribeBatteryPercentAsync"/>
        public Task<byte> GetBatteryPercentAsync()
        {
            return connection.SendCommandAsync(GetBatteryPercent());
        }

        /// <summary>
        /// Sends a message to the hub to subscribe to battery level changes.
        /// </summary>
        /// <param name="observer">Receives change notifications.</param>
        /// <returns>A task with a <see cref="IDisposable"/> for unsubscribing.</returns>
        /// <remarks>The task returns once the subscribe message has been sent.
        /// The observer will receive <see cref="IObserver{T}.OnNext(T)"/> notifications
        /// until the returned <see cref="IDisposable"/> is disposed or there is
        /// a communication error. <see cref="IObserver{T}.OnCompleted()"/> will
        /// be called if the unsubscribe command is sucessfully sent (when
        /// <see cref="IDisposable.Dispose()"/> is called) or
        /// <see cref="IObserver{T}.OnError(Exception)"/> is called if there is an error.
        /// </remarks>
        /// <seealso cref="GetBatteryPercentAsync"/>
        public Task<IDisposable> SubscribeBatteryPercentAsync(IObserver<byte> observer)
        {
            return connection.SubscribeCommandAsync(SubscribeBatteryPercent(), UnsubscribeBatteryPercent(), observer);
        }

        /// <summary>
        /// Sends a command to the hub to get the manufacturer.
        /// </summary>
        /// <returns>A task that awaits a reply from the hub containing
        /// the manufacturer.</returns>
        public Task<string> GetManufacturerAsync()
        {
            return connection.SendCommandAsync(GetManufacturer());
        }

        /// <summary>
        /// Sends a command to the hub to get the Bluetooth chip vendor firmware version.
        /// </summary>
        /// <returns>A task that awaits a reply from the hub containing
        /// the Bluetooth chip firmware version.</returns>
        public Task<string> GetBluetoothFirmwareVersion()
        {
            return connection.SendCommandAsync(CommandFactory.GetBluetoothFirmwareVersion());
        }

        // TODO: delete this method
        public Task<ushort> GetUnknownAsync()
        {
            ReadOnlyMemory<byte> message = new byte[] { 0x05, 0x00, 0x01, 0x0a, 0x05 };
            var command = new Internal.Command<ushort>(message, Internal.RequestTypes.Reply, r => {
                return System.Buffers.Binary.BinaryPrimitives.ReadUInt16LittleEndian(r.Slice(5, 2));
            });
            return connection.SendCommandAsync(command);
        }

        /// <summary>
        /// Sends a command to the hub to get the hub type.
        /// </summary>
        /// <returns>A task that awaits a reply from the hub containing
        /// the hub type.</returns>
        public Task<HubType> GetHubTypeAsync()
        {
            return connection.SendCommandAsync(GetHubType());
        }

        // TODO: delete this method
        // was guessing that this has to do with remote control channel, but doesn't look like it
        public Task<byte> GetChannelAsync()
        {
            ReadOnlyMemory<byte> message = new byte[] { 0x05, 0x00, 0x01, 0x0c, 0x05 };
            var command = new Internal.Command<byte>(message, Internal.RequestTypes.Reply, r => {
                return r[5];
            });
            return connection.SendCommandAsync(command);
        }

        /// <summary>
        /// Sends a command to the hub to get the Bluetooth address.
        /// </summary>
        /// <returns>A task that awaits a reply from the hub containing
        /// the Bluetooth address.</returns>
        public Task<BluetoothAddress> GetBluetoothAddressAsync()
        {
            return connection.SendCommandAsync(GetBluetoothAddress());
        }

        /// <summary>
        /// Sends a command to the hub to get the bootloader Bluetooth address.
        /// </summary>
        /// <returns>A task that awaits a reply from the hub containing
        /// the bootloader Bluetooth address.</returns>
        /// <remarks>This is the Bluetooth address used by the hub when it is
        /// booted in firmware update mode.</remarks>
        public Task<BluetoothAddress> GetBootloaderBluetoothAddressAsync()
        {
            return connection.SendCommandAsync(GetBootloaderBluetoothAddress());
        }
    }
}
