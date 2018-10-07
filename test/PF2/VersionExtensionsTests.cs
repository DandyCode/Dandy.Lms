using System;
using NUnit.Framework;

namespace Dandy.Lms.PF2.Test
{
    public class VersionExtensionsTests
    {
        [Test]
        public void TestReadVersionLittleEndian()
        {
            ReadOnlySpan<byte> source = new byte[] { 0x78, 0x56, 0x34, 0x12 };
            Assert.That(source.ReadVersionLittleEndian(), Is.EqualTo(new Version(1, 2, 34, 5678)));

            Assert.That(() => {
                ReadOnlySpan<byte> bad = new byte[3];
                // requires 4 bytes - 3 is too short
                bad.ReadVersionLittleEndian();
            }, Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void TestToPF2Format()
        {
            // make sure leading zeros work
            Assert.That(new Version(1, 2, 3, 4).ToPF2Format(), Is.EqualTo("1.2.03.0004"));
        }
    }
}
