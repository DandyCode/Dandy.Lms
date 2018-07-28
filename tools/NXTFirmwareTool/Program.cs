using System;

using Dandy.LMS.NXT;

namespace NXTFirmwareTool
{
    class Program
    {
        static Brick nxt;

        static void handleError(ErrorException ex)
        {
            Console.WriteLine("Error: {0}", ex.Message);
            nxt?.Close();
            Environment.Exit((int)ex.Error);
        }

        static void Main(string[] args)
        {
            if (args.Length != 1) {
                Console.Error.WriteLine("Missing firmware name argument");
                Environment.Exit(1);
            }

            var fwFile = args[0];
            Console.Write("Checking firmware... ");
            try {
                Firmware.Validate(fwFile);
                Console.WriteLine("OK.");
            }
            catch (ErrorException ex) {
                handleError(ex);
            }

            try {
                nxt = new Brick();
            }
            catch (ErrorException ex) {
                handleError(ex);
            }

            try {
                nxt.Find();
            }
            catch (ErrorException ex) when (ex.Error == Error.NotPresent) {
                Console.Error.WriteLine("NXT not found. Is it properly plugged in via USB?");
                Environment.Exit(1);
            }
            catch (ErrorException ex) {
                handleError(ex);
            }

            if (!nxt.IsInResetMode) {
                Console.Error.WriteLine("NXT found, but not running in reset mode.");
                Console.Error.WriteLine("Please reset your NXT manually and restart this program.");
                Environment.Exit(2);
            }

            try {
                nxt.Open();
            }
            catch (ErrorException ex) {
                handleError(ex);
            }

            Console.WriteLine("NXT device in reset mode located and opened.");
            Console.WriteLine("Starting firmware flash procedure now...");

            try {
                nxt.Flash(fwFile);
            }
            catch (ErrorException ex) {
                handleError(ex);
            }

            Console.WriteLine("Firmware flash complete.");

            try {
                nxt.Jump(0x00100000);
            }
            catch (ErrorException ex) {
                handleError(ex);
            }

            Console.WriteLine("New firmware started!");

            try {
                nxt.Close();
            }
            catch (ErrorException ex) {
                handleError(ex);
            }
        }
    }
}
