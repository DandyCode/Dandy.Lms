using System;
using System.Buffers.Binary;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Text;
using Dandy.Devices.BluetoothLE;
using Dandy.Lms.Internal;

namespace Dandy.Lms.PF2
{
    /// <summary>
    /// Class for generating Powered Up protocol commands.
    /// </summary>
    public static class CommandFactory
    {
        enum Cmd : byte
        {
            DeviceInfo = 1,
            PortInfo = 4,
        }

        enum ReplyType : byte
        {
            SetValue = 1,
            Subscribe = 2,
            Unsubscibe = 3,
            SetDefault = 4,
            GetValue = 5,
        }

        enum ReplyStatus : byte
        {
            Error = 5,
            Success = 6,
        }

        static Memory<byte> CreateMessage<TSubcmd>(Cmd cmd, TSubcmd subcmd, ReplyType replyType, int dataLength = 0) where TSubcmd : Enum
        {
            if (dataLength < 0) {
                throw new ArgumentOutOfRangeException(nameof(dataLength));
            }

            var message = new byte[5 + dataLength];
            message[0] = (byte)message.Length;
            message[1] = 0;
            message[2] = (byte)cmd;
            message[3] = Convert.ToByte(subcmd);
            message[4] = (byte)replyType;

            return message;
        }

        static TReturn ParseReply<TReturn, TSubcmd>(ReadOnlySpan<byte> reply, Cmd cmd, TSubcmd subcmd, Command<TReturn>.ReplyParser parseReturnValue) where TSubcmd : Enum
        {
            if (reply.Length < 1) {
                throw new ArgumentException("Reply is empty", nameof(reply));
            }

            if (reply[0] != reply.Length) {
                throw new ArgumentException("Bad length", nameof(reply));
            }

            if ((Cmd)reply[2] != cmd) {
                throw new ArgumentException("Command does not match", nameof(reply));
            }

            if (reply[3] != Convert.ToByte(subcmd)) {
                throw new ArgumentException("Subcommand does not match", nameof(reply));
            }

            if ((ReplyStatus)reply[4] != ReplyStatus.Success) {
                // TODO: need a better exception
                throw new Exception("Command failed");
            }

            return parseReturnValue(reply);
        }

        #region Device Information commands

        enum DeviceInfoSubcmd : byte
        {
            Name = 1,
            Button = 2,
            FirmwareVersion = 3,
            HardwareVersion = 4,
            RSSI = 5,
            Battery = 6,
            Manufacturer = 8,
            BlueNRG = 9,
            HubType = 11,
            BDAddress = 13,
            BootloaderBDAddres = 14,
        }

        /// <summary>
        /// Creates a new command that gets the name of programmable brick.
        /// </summary>
        /// <returns>The command.</returns>
        public static Command<string> GetName()
        {
            var message = CreateMessage(Cmd.DeviceInfo, DeviceInfoSubcmd.Name, ReplyType.GetValue);

            return new Command<string>(message, RequestTypes.Reply, r => {
                return ParseReply(r, Cmd.DeviceInfo, DeviceInfoSubcmd.Name, reply => {
                    // FIXME: there should be a Span-based overload of GetString()
                    return Encoding.ASCII.GetString(reply.Slice(5).ToArray());
                });
            });
        }

        /// <summary>
        /// Creates a new command that sets the name of programmable brick.
        /// </summary>
        /// <returns>The command.</returns>
        public static Command<NoReply> SetName(string name)
        {
            if (name == null) {
                throw new ArgumentNullException(nameof(name));
            }

            if (string.IsNullOrWhiteSpace(name)) {
                throw new ArgumentException("Empty name", nameof(name));
            }

            // longer names are ignored by firmware
            if (name.Length > 14) {
                throw new ArgumentException("Name is too long", nameof(name));
            }

            var message = CreateMessage(Cmd.DeviceInfo, DeviceInfoSubcmd.Name, ReplyType.SetValue, name.Length);
            Encoding.ASCII.GetBytes(name).AsSpan().CopyTo(message.Span.Slice(5));

            return new Command<NoReply>(message, RequestTypes.NoReply, r => {
                throw new InvalidOperationException("set name command does not send a reply");
            });
        }

        /// <summary>
        /// Creates a new command that subscribes to name changes of programmable brick.
        /// </summary>
        /// <returns>The command.</returns>
        public static Command<string> SubscribeName()
        {
            var message = CreateMessage(Cmd.DeviceInfo, DeviceInfoSubcmd.Name, ReplyType.Subscribe);

            return new Command<string>(message, RequestTypes.Subscribe, r => {
                return ParseReply(r, Cmd.DeviceInfo, DeviceInfoSubcmd.Name, reply => {
                    // FIXME: there should be a Span-based overload of GetString()
                    return Encoding.ASCII.GetString(reply.Slice(5).ToArray());
                });
            });
        }

        /// <summary>
        /// Creates a new command that subscribes to name changes of programmable brick.
        /// </summary>
        /// <returns>The command.</returns>
        public static Command<NoReply> UnsubscribeName()
        {
            var message = CreateMessage(Cmd.DeviceInfo, DeviceInfoSubcmd.Name, ReplyType.Unsubscibe);

            return new Command<NoReply>(message, RequestTypes.NoReply, r => {
                return ParseReply(r, Cmd.DeviceInfo, DeviceInfoSubcmd.Name, reply => default(NoReply));
            });
        }

        // TODO: there are also commands to subscribe/unsubscribe Name notifications and set default name

        /// <summary>
        /// Creates a new command to get the button state.
        /// </summary>
        /// <returns>a new command</returns>
        public static Command<bool> GetButtonState()
        {
            var message = CreateMessage(Cmd.DeviceInfo, DeviceInfoSubcmd.Button, ReplyType.GetValue);

            return new Command<bool>(message, RequestTypes.Reply, r => {
                return ParseReply(r, Cmd.DeviceInfo, DeviceInfoSubcmd.Name, reply => Convert.ToBoolean(reply[5]));
            });
        }

        // TODO: can also subscribe/unsubscribe from button value

        /// <summary>
        /// Creates a new command to get the firmware version.
        /// </summary>
        /// <returns>A new command</returns>
        public static Command<Version> GetFirmwareVersion()
        {
            var message = CreateMessage(Cmd.DeviceInfo, DeviceInfoSubcmd.FirmwareVersion, ReplyType.GetValue);

            return new Command<Version>(message, RequestTypes.Reply, r => {
                return ParseReply(r, Cmd.DeviceInfo, DeviceInfoSubcmd.FirmwareVersion, reply => {
                    return VersionExtensions.ReadVersionLittleEndian(reply.Slice(5, 4));
                });
            });
        }

        /// <summary>
        /// Creates a new command to get the hardware version.
        /// </summary>
        /// <returns>A new command</returns>
        public static Command<Version> GetHardwareVersion()
        {
            var message = CreateMessage(Cmd.DeviceInfo, DeviceInfoSubcmd.HardwareVersion, ReplyType.GetValue);

            return new Command<Version>(message, RequestTypes.Reply, r => {
                return ParseReply(r, Cmd.DeviceInfo, DeviceInfoSubcmd.HardwareVersion, reply => {
                    return VersionExtensions.ReadVersionLittleEndian(reply.Slice(5, 4));
                });
            });
        }

        /// <summary>
        /// Creates a new command to get the RSSI???
        /// </summary>
        /// <returns>A new command.</returns>
        public static Command<sbyte> GetRSSI()
        {
            var message = CreateMessage(Cmd.DeviceInfo, DeviceInfoSubcmd.RSSI, ReplyType.GetValue);

            return new Command<sbyte>(message, RequestTypes.Reply, r => {
                return ParseReply(r, Cmd.DeviceInfo, DeviceInfoSubcmd.RSSI, reply => {
                    return (sbyte)reply[5];
                });
            });
        }

        // TODO: RSSI can also be subscribed/unsubscribed

        /// <summary>
        /// Creates a new command to get battery level in percent full.
        /// </summary>
        /// <returns>A new command.</returns>
        public static Command<byte> GetBatteryPercent()
        {
            var message = CreateMessage(Cmd.DeviceInfo, DeviceInfoSubcmd.Battery, ReplyType.GetValue);

            return new Command<byte>(message, RequestTypes.Reply, r => {
                return ParseReply(r, Cmd.DeviceInfo, DeviceInfoSubcmd.Battery, reply => {
                    return reply[5];
                });
            });
        }

        // TODO: Battery can also be subscribed/unsubscribed

        /// <summary>
        /// Creates a new command to get the manufacturer name.
        /// </summary>
        /// <returns>A new command.</returns>
        public static Command<string> GetManufacturer()
        {
            var message = CreateMessage(Cmd.DeviceInfo, DeviceInfoSubcmd.Manufacturer, ReplyType.GetValue);

            return new Command<string>(message, RequestTypes.Reply, r => {
                return ParseReply(r, Cmd.DeviceInfo, DeviceInfoSubcmd.Manufacturer, reply => {
                    // FIXME: there should be a Span-based overload of GetString()
                    return Encoding.ASCII.GetString(reply.Slice(5).ToArray());
                });
            });
        }

        /// <summary>
        /// Creates a new command to get the Bluetooth software version.
        /// </summary>
        /// <returns>A new command.</returns>
        public static Command<string> GetBluetoothSoftwareVersion()
        {
            var message = CreateMessage(Cmd.DeviceInfo, DeviceInfoSubcmd.BlueNRG, ReplyType.GetValue);

            return new Command<string>(message, RequestTypes.Reply, r => {
                return ParseReply(r, Cmd.DeviceInfo, DeviceInfoSubcmd.BlueNRG, reply => {
                    // FIXME: there should be a Span-based overload of GetString()
                    return Encoding.ASCII.GetString(reply.Slice(5).ToArray());
                });
            });
        }

        /// <summary>
        /// Creates a new command to get hub type id.
        /// </summary>
        /// <returns>A new command.</returns>
        public static Command<HubType> GetHubType()
        {
            var message = CreateMessage(Cmd.DeviceInfo, DeviceInfoSubcmd.HubType, ReplyType.GetValue);

            return new Command<HubType>(message, RequestTypes.Reply, r => {
                return ParseReply(r, Cmd.DeviceInfo, DeviceInfoSubcmd.HubType, reply => {
                    return (HubType)reply[5];
                });
            });
        }

        /// <summary>
        /// Creates a new command to get Bluetooth address.
        /// </summary>
        /// <returns>A new command.</returns>
        public static Command<BluetoothAddress> GetBDAddress()
        {
            var message = CreateMessage(Cmd.DeviceInfo, DeviceInfoSubcmd.BDAddress, ReplyType.GetValue);

            return new Command<BluetoothAddress>(message, RequestTypes.Reply, r => {
                return ParseReply(r, Cmd.DeviceInfo, DeviceInfoSubcmd.BDAddress, reply => {
                    return BluetoothAddress.FromSpan(reply.Slice(5, 6), true);
                });
            });
        }

        /// <summary>
        /// Creates a new command to get Bluetooth address for the bootloader.
        /// </summary>
        /// <returns>A new command.</returns>
        public static Command<BluetoothAddress> GetBootloaderBDAddress()
        {
            var message = CreateMessage(Cmd.DeviceInfo, DeviceInfoSubcmd.BootloaderBDAddres, ReplyType.GetValue);

            return new Command<BluetoothAddress>(message, RequestTypes.Reply, r => {
                return ParseReply(r, Cmd.DeviceInfo, DeviceInfoSubcmd.BootloaderBDAddres, reply => {
                    return BluetoothAddress.FromSpan(reply.Slice(5, 6), true);
                });
            });
        }

        #endregion
    }
}
