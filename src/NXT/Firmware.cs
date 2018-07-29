using System;
using System.IO;
using System.Threading.Tasks;

namespace Dandy.Lms.Nxt
{
    public static class Firmware
    {
        public static async Task FlashPrepareAsync(this Samba nxt)
        {
            // Put the clock in PLL/2 mode
            await nxt.WriteWordAsync(0xFFFFFC30, 0x7);

            // Unlock the flash chip
            await nxt.UnlockAllRegionsAsync();

            // Send the flash writing routine
            await nxt.SendFileAsync(0x202000, Nxt.Flash.Bin);
        }

        static async Task FlashBlockAsync(this Samba nxt, uint blockNum, byte[] buf)
        {
            // Set the target block number
            await nxt.WriteWordAsync(0x202300, blockNum);

            // Send the block to flash
            await nxt.SendFileAsync(0x202100, buf);

            // Jump into the flash writing routine
            await nxt.GoAsync(0x202000);
        }

        static Task FlashFinishAsync(this Samba nxt)
        {
            return nxt.WaitReadyAsync();
        }

        public static void Validate(byte[] data)
        {
            if (data == null) {
                throw new ArgumentNullException(nameof(data));
            }
            if (data.Length > 256 * 1024) {
                throw new ArgumentException("Firmware is too big", nameof(data));
            }
        }

        public static async Task FlashAsync(this Samba nxt, byte[] data)
        {
            using (var f = new MemoryStream(data)) {
                Validate(data);
                await nxt.FlashPrepareAsync();

                for (var i = 0u; i < 1024; i++) {
                    var buf = new byte[256];
                    var ret = f.Read(buf, 0, buf.Length);
                    await nxt.FlashBlockAsync(i, buf);
                    if (ret < 256) {
                        await nxt.FlashFinishAsync();
                        return;
                    }
                }
            }
            await nxt.FlashFinishAsync();
        }
    }
}
