using System;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Dandy.Lms.Nxt;
using Windows.Storage;

namespace Dandy.Lms.NxtFirmwareTool.Uwp
{
    class Program
    {
        static IDisposable nxt;

        static void handleError(Exception ex)
        {
            Console.WriteLine("Error: {0}", ex.Message);
            nxt?.Dispose();
            Environment.Exit(1);
        }

        static async Task Main(string[] args)
        {
            if (args.Length != 1) {
                Console.Error.WriteLine("Missing firmware name argument");
                Environment.Exit(1);
            }

            var fwFile = args[0];
            byte[] fwData = null;
            Console.Write("Checking firmware... ");
            try {
                var file = await StorageFile.GetFileFromPathAsync(fwFile);
                var buf = await FileIO.ReadBufferAsync(file);
                fwData = buf.ToArray();
                Firmware.Validate(fwData);
                Console.WriteLine("OK.");
            }
            catch (Exception ex) {
                handleError(ex);
            }

            Samba device = null;

            try {
                var devices = await Samba.FindAllAsync();
                device = devices.FirstOrDefault();
                if (device == null) {
                    Console.Error.WriteLine("NXT not found. Is it properly plugged in via USB?");
                    Environment.Exit(1);
                }
            }
            catch (Exception ex) {
                handleError(ex);
            }

            try {
                nxt = await device.OpenAsync();
            }
            catch (Exception ex) {
                handleError(ex);
            }

            Console.WriteLine("NXT device in reset mode located and opened.");
            Console.WriteLine("Starting firmware flash procedure now...");

            try {
                await device.FlashAsync(fwData);
            }
            catch (Exception ex) {
                handleError(ex);
            }

            Console.WriteLine("Firmware flash complete.");

            try {
                await device.GoAsync(0x00100000);
            }
            catch (Exception ex) {
                handleError(ex);
            }

            Console.WriteLine("New firmware started!");

            try {
                nxt.Dispose();
            }
            catch (Exception ex) {
                handleError(ex);
            }
        }
    }
}
