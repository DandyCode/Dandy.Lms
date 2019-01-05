using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dandy.Devices.BluetoothLE;
using McMaster.Extensions.CommandLineUtils;
using ShellProgressBar;

namespace PF2FirmwareTool
{
    [Command("pf2-fw-tool", Description = "Unofficial firmware tool for LEGO BOOST and Powered Up devices",
        ThrowOnUnexpectedArgument = false)]
    [Subcommand("flash-fw", typeof(FlashFirmware))]
    [Subcommand("get-rdp", typeof(GetRDP))]
    class Program
    {
        public static int Main(string[] args) => CommandLineApplication.Execute<Program>(args);

        private int OnExecute()
        {
            Console.Error.WriteLine("Missing or invalid command. Run 'pf2-fw-tool --help' for a list of commands.");

            return 1;
        }

        [Command(Description = "Write firmware to flash memory.")]
        class FlashFirmware : Subcommand
        {
            [Argument(0, "<file>", "Firmware file name")]
            [Required]
            [FileExists]
            string FirmwareFile { get; set; }

            protected override async Task<int> OnConnection(GattCharacteristic lwpChar)
            {
                Console.WriteLine("Reading device info...");

                Memory<byte> data = await File.ReadAllBytesAsync(FirmwareFile);
                var memory = new MemoryStream();
                var writer = new BinaryWriter(memory);

                writer.Write((byte)0x55);

                await lwpChar.WriteValueAsync(new Memory<byte>(memory.GetBuffer(), 0, 5), GattWriteOption.WriteWithoutResponse);
                var reply = await GetValueAsync();

                if (reply.Span[0] != 0x55) {
                    Console.Error.WriteLine("Invalid comand 0x55.");
                    return 1;
                }

                // TODO: reply.Slice(1, 4) is probably bootloader version, is it important?

                var intBytes = reply.Slice(5, 4).ToArray();
                if (!BitConverter.IsLittleEndian) {
                    Array.Reverse(intBytes);
                }
                var startAddr = BitConverter.ToInt32(intBytes);

                intBytes = reply.Slice(9, 4).ToArray();
                if (!BitConverter.IsLittleEndian) {
                    Array.Reverse(intBytes);
                }
                var endAddr = BitConverter.ToInt32(intBytes);

                // TODO: should we check file size against address or just trust
                // the 0x44 command below?

                // TODO reply.Span[13] is probably the device ID, is there a way
                // to make sure the firmware file is valid for this devices?

                if (reply.Span[1] != 0) {
                    Console.WriteLine(reply.ToArray()[1]);
                    Console.Error.WriteLine("Firmware file is too large.");
                    return 1;
                }

                memory.Position = 0;
                writer.Write((byte)0x44);
                writer.Write(data.Length);

                await lwpChar.WriteValueAsync(new Memory<byte>(memory.GetBuffer(), 0, 5), GattWriteOption.WriteWithoutResponse);
                reply = await GetValueAsync();

                if (reply.Span[0] != 0x44) {
                    Console.Error.WriteLine("Invalid comand 0x44.");
                    return 1;
                }

                if (reply.Span[1] != 0) {
                    Console.WriteLine(reply.ToArray()[1]);
                    Console.Error.WriteLine("Firmware file is too large.");
                    return 1;
                }

                Console.WriteLine("Erasing flash memory...");

                memory.Position = 0;
                writer.Write((byte)0x11);

                await lwpChar.WriteValueAsync(new Memory<byte>(memory.GetBuffer(), 0, 1), GattWriteOption.WriteWithoutResponse);
                reply = await GetValueAsync();

                if (reply.Span[0] != 0x11) {
                    Console.Error.WriteLine("Invalid comand 0x11.");
                    return 1;
                }

                // not sure why we get a short reply sometimes
                if (reply.Length > 1 && reply.Span[1] != 0) {
                    Console.Error.WriteLine("Erasing failed.");
                    return 1;
                }

                // only 14 data bytes fit in each packet
                using (var progress = new ProgressBar(data.Length / 14, "Flashing new firmware...")) {
                    for (int i = 0; i < data.Length; i += 14) {
                        var size = Math.Min(data.Length - i, 14);
                        memory.Position = 0;
                        writer.Write((byte)0x22);
                        writer.Write((byte)size);
                        writer.Write(startAddr + i);
                        writer.Write(data.Slice(i, size).ToArray());

                        await lwpChar.WriteValueAsync(new Memory<byte>(memory.GetBuffer(), 0, size + 6), GattWriteOption.WriteWithoutResponse);
                        progress.Tick();
                    }

                    // for some reason, there is a bit of delay for this call, at least on Linux
                    reply = await GetValueAsync();
                }

                if (reply.Span[0] != 0x22) {
                    Console.Error.WriteLine("Invalid comand 0x22.");
                    return 1;
                }

                var checksum = reply.Span[1];
                intBytes = reply.Slice(2, 4).ToArray();
                if (!BitConverter.IsLittleEndian) {
                    Array.Reverse(intBytes);
                }
                var written = BitConverter.ToInt32(intBytes);
                Console.WriteLine($"Checksum: {checksum}, Bytes written: {written}");

                Console.WriteLine("Rebooting...");

                await lwpChar.WriteValueAsync(new byte[] { 0x33 }, GattWriteOption.WriteWithoutResponse);

                return 0;
            }
        }

        [Command(Description = "Read the RDP option byte.",
            ExtendedHelpText = "This indicates the protection level set on the CPU." +
            " Level 2 means that the SWD (debug) port is permanently disabled. See" +
            " STM32F0 manual for more info. Note: this command is not available on" +
            " all devices")]
        class GetRDP : Subcommand
        {
            protected override async Task<int> OnConnection(GattCharacteristic lwpChar)
            {
                Memory<byte> msg = new byte[] { 0x77 };

                await lwpChar.WriteValueAsync(msg);
                var reply = await GetValueAsync();

                if (reply.Span[0] != 0x77) {
                    Console.Error.WriteLine("Command failed.");
                    return 1;
                }

                Console.WriteLine($"RDP Level {reply.Span[1]}");
                return 0;
            }
        }

        sealed class AdvertismentWatcherException : System.Exception
        {
            public AdvertismentWatcherException(AdvertisementWatcherError error) {
                Error = error;
            }

            public AdvertisementWatcherError Error { get; }
        }

        abstract class Subcommand
        {
            static readonly Guid bootloaderServiceUuid = new Guid("00001625-1212-efde-1623-785feabcd123");
            static readonly Guid bootloaderCharacteristicUuid = new Guid("00001626-1212-efde-1623-785feabcd123");

            readonly ManualResetEvent notifyEvent = new ManualResetEvent(false);

            Memory<byte> notifyValue;

            protected abstract Task<int> OnConnection(GattCharacteristic lwpChar);

            public async Task<int> OnExecuteAsync()
            {
                // Progress bar looks better when it isn't on the bottom line
                Console.Clear();

                Console.WriteLine("Press and hold the green button for 5 seconds to activate the firmware upload program on the device.");
                Console.WriteLine("Waiting for connection...");

                try {
                    var info = await GetDeviceInfoAsync();
                    Console.WriteLine(info.advertisement.LocalName);
                    Console.WriteLine(info.address);

                    using (var device = await Device.FromAddressAsync(info.address)) {
                        var services = await device.GetGattServicesAsync(bootloaderServiceUuid);
                        var lwpService = services.Single(x => x.Uuid == bootloaderServiceUuid);
                        var characteristics = await lwpService.GetCharacteristicsAsync(bootloaderCharacteristicUuid);
                        var lwpChar = characteristics.Single();
                        lwpChar.ValueChanged += (s, e) => {
                            notifyValue = e.Value;
                            notifyEvent.Set();
                        };
                        await lwpChar.StartNotifyAsync();
                        return await OnConnection(lwpChar);
                    }
                }
                catch (AdvertismentWatcherException ex) {
                    switch (ex.Error) {
                    case AdvertisementWatcherError.TurnedOff:
                        Console.Error.WriteLine("Bluetooth is turned off. Turn it on and try again.");
                        break;
                    case AdvertisementWatcherError.Unauthorized:
                        Console.Error.WriteLine("Not authorized to use Bluetooth.");
                        break;
                    case AdvertisementWatcherError.Unsupported:
                        Console.Error.WriteLine("Bluetooth not supported on this platform.");
                        break;
                    default:
                        Console.Error.WriteLine("Unknown Bluetooth error.");
                        break;
                    }
                    return 1;
                }
            }

            protected Task<Memory<byte>> GetValueAsync()
            {
                return Task.Run(() => {
                    // TODO: should probably have timeout here
                    notifyEvent.WaitOne();
                    notifyEvent.Reset();
                    return notifyValue;
                });
            }

            Task<(Advertisement advertisement, BluetoothAddress address)> GetDeviceInfoAsync()
            {
                var source = new TaskCompletionSource<(Advertisement, BluetoothAddress)>();

                var watcher = new AdvertisementWatcher(bootloaderServiceUuid);
                watcher.Received += (s, e) => {
                    source.TrySetResult((e.Advertisement, e.Address));
                    watcher.Stop();
                };

                watcher.Stopped += (s, e) => {
                    if (e.Error != AdvertisementWatcherError.None) {
                        source.TrySetException(new AdvertismentWatcherException(e.Error));
                    }
                    else {
                        source.TrySetCanceled();
                    }
                };

                watcher.Start();

                return source.Task;
            }
        }
    }
}
