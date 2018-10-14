using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dandy.Devices.BluetoothLE;
using Dandy.Lms.Internal;

namespace Dandy.Lms.PF2
{
    /// <summary>
    /// Class to manage a Bluetooth LE connection to a Powered Up
    /// programmable brick.
    /// </summary>
    public sealed class BLEConnection : INotifyPropertyChanged
    {
        /// <summary>
        /// The Bluetooth GATT service UUID for LEGO Powered Up hub.
        /// </summary>
        internal static readonly Guid ServiceUuid = new Guid("00001623-1212-efde-1623-785feabcd123");

        /// <summary>
        /// The Bluetooth GATT charactaristic UUID for LEGO Powered Up hub.
        /// </summary>
        internal static readonly Guid CharacteristicUuid = new Guid("00001624-1212-efde-1623-785feabcd123");

        private readonly Device device;
        private readonly GattCharacteristic characteristic;
        private readonly ConcurrentDictionary<(byte cmd, byte subcmd), Action<ReadOnlyMemory<byte>>> replyHandlers;

        /// <summary>
        /// Gets the Blueooth address.
        /// </summary>
        public BluetoothAddress Address => device.BluetoothAddress;

        /// <summary>
        /// Gets the name of the Bluetooth device.
        /// </summary>
        public string Name => device.Name;

        /// <inheritdoc/>
        public event PropertyChangedEventHandler PropertyChanged;

        BLEConnection(Device device, GattCharacteristic characteristic)
        {
            this.device = device ?? throw new ArgumentNullException(nameof(device));
            device.PropertyChanged += Device_PropertyChanged;
            this.characteristic = characteristic ?? throw new ArgumentNullException(nameof(characteristic));
            characteristic.ValueChanged += Characteristic_ValueChanged;
            replyHandlers = new ConcurrentDictionary<(byte, byte), Action<ReadOnlyMemory<byte>>>();
        }

        private void Device_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName) {
            case nameof(Device.Name):
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
                break;
            }
        }

        private void Characteristic_ValueChanged(object sender, GattValueChangedEventArgs e)
        {
            var cmd = e.Value.Span[2];
            var subcmd = e.Value.Span[3];
            if (replyHandlers.TryGetValue((cmd, subcmd), out var handler)) {
                handler(e.Value);
            }
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
        /// Sends a command to the programmable brick without the brick sending a reply.
        /// </summary>
        /// <typeparam name="T">The return parameter(s) of the reply.</typeparam>
        /// <param name="command">The command to send.</param>
        /// <returns>A <see cref="Task"/> that completes when the comamnd has been sent.</returns>
        public Task SendCommandNoReplyAsync<T>(Command<T> command)
        {
            if (command == null) {
                throw new ArgumentNullException(nameof(command));
            }

            if (!command.RequestTypes.HasFlag(RequestTypes.NoReply)) {
                throw new ArgumentException("This command must receive a reply", nameof(command));
            }

            return characteristic.WriteValueAsync(command.Message, GattWriteOption.WriteWithoutResponse);
        }

        /// <summary>
        /// Sends a command to the programmable brick and awaits a reply.
        /// </summary>
        /// <typeparam name="T">The return parameter(s) of the reply.</typeparam>
        /// <param name="command">The command to send.</param>
        /// <returns>The reply value</returns>
        public async Task<T> SendCommandAsync<T>(Command<T> command)
        {
            if (command == null) {
                throw new ArgumentNullException(nameof(command));
            }

            if (!command.RequestTypes.HasFlag(RequestTypes.Reply)) {
                throw new ArgumentException("This command cannot receive a reply", nameof(command));
            }

            var cmd = command.Message.Span[2];
            var subcmd = command.Message.Span[3];
            var source = new TaskCompletionSource<ReadOnlyMemory<byte>>();

            void handleReply(ReadOnlyMemory<byte> r)
            {
                source.TrySetResult(r);
                replyHandlers.TryRemove((cmd, subcmd), out var _);
            }

            if (!replyHandlers.TryAdd((cmd, subcmd), handleReply)) {
                throw new InvalidOperationException("An identical command is already pending.");
            }

            await characteristic.WriteValueAsync(command.Message, GattWriteOption.WriteWithoutResponse);

            var reply = await source.Task;
            return command.ParseReplay(reply.Span);
        }

        /// <summary>
        /// Sends a command to the programmable brick and awaits multiple replies.
        /// </summary>
        /// <typeparam name="T">The return parameter(s) of the reply.</typeparam>
        /// <param name="subscribe">The subscribe command to send.</param>
        /// <param name="unsubscribe">The unsubscribe command to send when the returned
        /// <see cref="IDisposable"/> is disposed.</param>
        /// <param name="observer">An observer to receive replies.</param>
        /// <returns>A <see cref="Task{IDisposable}"/> that completes when the
        /// <paramref name="subscribe"/> command has been sent.</returns>
        public async Task<IDisposable> SubscribeCommandAsync<T>(Command<T> subscribe, Command<NoReply> unsubscribe, IObserver<T> observer)
        {
            if (subscribe == null) {
                throw new ArgumentNullException(nameof(subscribe));
            }

            if (unsubscribe == null) {
                throw new ArgumentNullException(nameof(unsubscribe));
            }

            if (observer == null) {
                throw new ArgumentNullException(nameof(observer));
            }

            if (!subscribe.RequestTypes.HasFlag(RequestTypes.Subscribe)) {
                throw new ArgumentException("This command is not a subscribe command", nameof(subscribe));
            }

            if (!unsubscribe.RequestTypes.HasFlag(RequestTypes.NoReply)) {
                throw new ArgumentException("Unsubscribe commands must allow no reply", nameof(unsubscribe));
            }

            var cmd = subscribe.Message.Span[2];
            var subcmd = subscribe.Message.Span[3];

            var disposer = new Disposer(() => {
                replyHandlers.TryRemove((cmd, subcmd), out var _);
                // TODO: should log exception if sending unsubscribe command fails
                characteristic.WriteValueAsync(unsubscribe.Message, GattWriteOption.WriteWithoutResponse)
                    .ContinueWith(t => observer.OnCompleted());
            });

            void handleReply(ReadOnlyMemory<byte> reply)
            {
                try {
                    observer.OnNext(subscribe.ParseReplay(reply.Span));
                }
                catch (Exception ex) {
                    replyHandlers.TryRemove((cmd, subcmd), out var _);
                    // TODO: should log exception if sending unsubscribe command fails
                    characteristic.WriteValueAsync(unsubscribe.Message, GattWriteOption.WriteWithoutResponse)
                        .ContinueWith(t => observer.OnError(ex));
                }
            }

            if (!replyHandlers.TryAdd((cmd, subcmd), handleReply)) {
                throw new InvalidOperationException("An identical command is already pending.");
            }

            await characteristic.WriteValueAsync(subscribe.Message, GattWriteOption.WriteWithoutResponse);

            return disposer;
        }

        class Disposer : IDisposable
        {
            private readonly Action dispose;

            public Disposer(Action dispose)
            {
                this.dispose = dispose ?? throw new ArgumentNullException(nameof(dispose));
            }

            public void Dispose() => dispose();
        }
    }
}
