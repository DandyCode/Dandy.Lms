using System.IO;

namespace Dandy.LMS.NXT
{
    public static class Firmware
    {
        public static void FlashPrepare(this Brick nxt)
        {
            // Put the clock in PLL/2 mode
            nxt.WriteWord(0xFFFFFC30, 0x7);

            // Unlock the flash chip
            nxt.UnlockAllRegions();

            // Send the flash writing routine
            nxt.SendFile(0x202000, NXT.Flash.Bin);
        }

        static void FlashBlock(this Brick nxt, uint blockNum, byte[] buf)
        {
            // Set the target block number
            nxt.WriteWord(0x202300, blockNum);

            // Send the block to flash
            nxt.SendFile(0x202100, buf);

            // Jump into the flash writing routine
            nxt.Jump(0x202000);
        }

        static void FlashFinish(this Brick nxt)
        {
            nxt.WaitReady();
        }

        static void Validate(FileInfo info)
        {
            try {
                if (info.Length > 256 * 1024) {
                    throw new ErrorException(Error.InvalidFirmware);
                }
            }
            catch (ErrorException) {
                throw;
            }
            catch {
                throw new ErrorException(Error.File);
            }
        }

        public static void Validate(string fwPath)
        {
            try {
                var info = new FileInfo(fwPath);
                using (info.OpenRead()) {
                    Validate(info);
                }
            }
            catch {
                throw new ErrorException(Error.File);
            }
        }

        public static void Flash(this Brick nxt, string fwPath)
        {
            try {
                var info = new FileInfo(fwPath);
                using (var f = info.OpenRead()) {
                    Validate(info);
                    nxt.FlashPrepare();

                    for (var i = 0u; i < 1024; i++) {
                        var buf = new byte[256];
                        var ret = f.Read(buf, 0, buf.Length);
                        nxt.FlashBlock(i, buf);
                        if (ret < 256) {
                            nxt.FlashFinish();
                            return;
                        }
                        nxt.FlashBlock(i, buf);
                    }
                }
                nxt.FlashFinish();
            }
            catch (ErrorException) {
                throw;
            }
            catch {
                throw new ErrorException(Error.File);
            }
        }
    }
}
