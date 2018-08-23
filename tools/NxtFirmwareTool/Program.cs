using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Dandy.Lms.Nxt;
using Dandy.Lms.Nxt.Commands;
using Dandy.Lms.Nxt.Enumeration;
using ShellProgressBar;

namespace Dandy.Lms.NxtFirmwareTool.Uwp
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            if (args.Length > 1) {
                Console.Error.WriteLine("Too many arguments");
                return 1;
            }

            if (args.Length < 1) {
                Console.Error.WriteLine("Missing firmware file argument");
                return 1;
            }

            var fwFile = args[0];
            byte[] fwData = null;
            Console.Write("Reading firmware file... ");
            try {
                fwData = File.ReadAllBytes(fwFile);
                Firmware.Validate(fwData);
                Console.WriteLine("OK.");
            }
            catch (Exception ex) {
                Console.Error.WriteLine(ex.Message);
                return 1;
            }

            Console.Write("Searching for NXTs... ");
            var findFwDevicesTask = Samba.FindAllAsync();
            var findNxtUsbDevicesTask = NxtDevice.FindAllUsbAsync();

            var fwDevices = (await findFwDevicesTask).ToList();
            var nxtDevices = (await findNxtUsbDevicesTask).ToList();

            var totalCount = fwDevices.Count + nxtDevices.Count;

            if (totalCount == 0) {
                Console.Error.WriteLine("None found.");
                return 1;
            }
            Console.WriteLine("OK.");

            var message = $"Finished flashing {{0}} of {totalCount}";

            using (var progressBar = new ProgressBar(totalCount, string.Format(message, 0))) {

                async Task rebootToFirmwareMode(NxtDevice device)
                {
                    using (var childProgressBar = progressBar.Spawn(1, "Starting firmware loader...")) {
                        try {
                            using (var nxt = await device.ConnectAsync()) {
                                if (await nxt.SendCommandAsync(SystemCommand.Boot())) {
                                    // TODO: search for firmware device
                                }
                                else {
                                    // TODO: error message
                                }
                            }
                        }
                        catch {

                        }
                    }
                }

                await Task.WhenAll(nxtDevices.Select(d => rebootToFirmwareMode(d)));

                async Task loadFirmware(Samba device)
                {
                    var childProgressBar = new ProgressHelper(progressBar, $"NXT on {device.PortName}");
                    try {
                        using (await device.OpenAsync()) {
                            // Write the firmware to flash memory
                            await device.FlashAsync(fwData, childProgressBar);
                            childProgressBar.ReportSuccess();
                            // reboot NXT
                            await device.GoAsync(0x00100000);
                        }
                    }
                    catch (Exception ex) {
                        childProgressBar.ReportError(ex.Message);
                    }
                    // hmm... possible race condition with progressBar.CurrentTick
                    progressBar.Tick(string.Format(message, progressBar.CurrentTick + 1));
                }

                await Task.WhenAll(fwDevices.Select(d => loadFirmware(d)));
            }

            return 0;
        }

        sealed class ProgressHelper : IProgress<int>
        {
            readonly ChildProgressBar child;

            public ProgressHelper(ProgressBar parent, string message)
            {
                child = parent.Spawn(1024, message, new ProgressBarOptions {
                    ForegroundColor = ConsoleColor.Yellow,
                    ForegroundColorDone = ConsoleColor.Green,
                    CollapseWhenFinished = false,
                });
            }

            void IProgress<int>.Report(int value)
            {
                child.Tick(value);
            }

            public void ReportSuccess()
            {
                child.Tick("Complete");
                child.Dispose();
            }

            public void ReportError(string message)
            {
                // progressBar.ForeGroundColor = ConsoleColor.Red;
                child.Tick($"Error: {message}");
            }
        }
    }
}
