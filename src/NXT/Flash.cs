namespace Dandy.LMS.NXT
{
    public static class Flash
    {
        public static readonly byte[] Bin = {
            0x21, 0xD8, 0xA0, 0xE3, 0x00, 0x40, 0x2D, 0xE9, 0x00, 0x00, 0x00,
            0xEB, 0x00, 0x80, 0xBD, 0xE8, 0x00, 0x20, 0xE0, 0xE3, 0x97, 0x30,
            0x12, 0xE5, 0x01, 0x00, 0x13, 0xE3, 0xFC, 0xFF, 0xFF, 0x0A, 0x02,
            0xC6, 0xA0, 0xE3, 0x0C, 0x00, 0xA0, 0xE1, 0x21, 0x0C, 0x80, 0xE2,
            0x02, 0xCA, 0x8C, 0xE2, 0x00, 0x10, 0xA0, 0xE3, 0x00, 0x33, 0x9C,
            0xE5, 0x03, 0x33, 0x81, 0xE0, 0x01, 0x21, 0x90, 0xE7, 0x03, 0x31,
            0xA0, 0xE1, 0x01, 0x10, 0x81, 0xE2, 0x01, 0x36, 0x83, 0xE2, 0x40,
            0x00, 0x51, 0xE3, 0x00, 0x20, 0x83, 0xE5, 0xF6, 0xFF, 0xFF, 0x1A,
            0x00, 0x33, 0x9C, 0xE5, 0x03, 0x3B, 0xA0, 0xE1, 0x23, 0x3B, 0xA0,
            0xE1, 0x03, 0x34, 0xA0, 0xE1, 0x5A, 0x34, 0x83, 0xE2, 0x01, 0x30,
            0x83, 0xE2, 0x00, 0x20, 0xE0, 0xE3, 0x9B, 0x30, 0x02, 0xE5, 0x97,
            0x30, 0x12, 0xE5, 0x01, 0x00, 0x13, 0xE3, 0xFC, 0xFF, 0xFF, 0x0A,
            0x1E, 0xFF, 0x2F, 0xE1
        };

        enum Commands
        {
            Lock = 0x2,
            Unlock = 0x4,
        }

        public static void WaitReady(this Brick nxt)
        {
            uint status;

            do {
                status = nxt.ReadWord(0xFFFFFF68);

                /* Bit 0 is the FRDY field. Set to 1 if the flash controller is
                * ready to run a new command.
                */
            } while ((status & 0x1) != 0x1);
        }

        static void AlterLock(this Brick nxt, int regionNum, Commands cmd)
        {
            var w =  0x5A000000 | (uint)((64 * regionNum) << 8);
            w += (uint)cmd;

            nxt.WaitReady();

            /* Flash mode register: FCMN 0x5, FWS 0x1
             * Flash command register: KEY 0x5A, FCMD = clear-lock-bit (0x4)
             * Flash mode register: FCMN 0x34, FWS 0x1
             */
            nxt.WriteWord(0xFFFFFF60, 0x00050100);
            nxt.WriteWord(0xFFFFFF64, w);
            nxt.WriteWord(0xFFFFFF60, 0x00340100);
        }

        static void LockRegion(this Brick nxt, int regionNum)
        {
            nxt.AlterLock(regionNum, Commands.Lock);
        }

        static void UnlockRegion(this Brick nxt, int regionNum)
        {
            nxt.AlterLock(regionNum, Commands.Unlock);
        }

        static void LockAllRegions(this Brick nxt)
        {
            for (var i = 0; i < 16; i++) {
                nxt.LockRegion(i);
            }
        }

        public static void UnlockAllRegions(this Brick nxt)
        {
            for (var i = 0; i < 16; i++) {
                nxt.UnlockRegion(i);
            }
        }
    }
}
