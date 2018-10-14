using System;
using NUnit.Framework;

using static Dandy.Lms.PF2.CommandFactory;

namespace Dandy.Lms.PF2.Test
{
    public class CommandFactoryTests
    {
        [Test]
        public void TestGetName()
        {
            var cmd = GetName();
            Assert.That(cmd.Message.ToArray(), Is.EquivalentTo(new byte[] {
                0x05, 0x00, 0x01, 0x01, 0x05
            }));
            var reply = cmd.ParseReplay(new byte[] {
                0x12, 0x00, 0x01, 0x01, 0x06, 0x4c, 0x45, 0x47, 0x4f,
                0x20, 0x4d, 0x6f, 0x76, 0x65, 0x20, 0x48, 0x75, 0x62
            });
            Assert.That(reply, Is.EqualTo("LEGO Move Hub"));
        }

        [Test]
        public void TestSetName()
        {
            var cmd = SetName("test name");
            Assert.That(cmd.Message.ToArray(), Is.EquivalentTo(new byte[] {
                0x0e, 0x00, 0x01, 0x01, 0x01, 0x74, 0x65,
                0x73, 0x74, 0x20, 0x6e, 0x61, 0x6d, 0x65
            }));
            Assert.That(() => {
                var reply = cmd.ParseReplay(new byte[] {
                    0x05, 0x00, 0x01, 0x01, 0x06
                });
            }, Throws.InvalidOperationException);
        }

        [Test]
        public void TestSubscribeName()
        {
            var cmd = SubscribeName();
            Assert.That(cmd.Message.ToArray(), Is.EquivalentTo(new byte[] {
                0x05, 0x00, 0x01, 0x01, 0x02
            }));
            var reply = cmd.ParseReplay(new byte[] {
                0x12, 0x00, 0x01, 0x01, 0x06, 0x4c, 0x45, 0x47, 0x4f,
                0x20, 0x4d, 0x6f, 0x76, 0x65, 0x20, 0x48, 0x75, 0x62
            });
            Assert.That(reply, Is.EqualTo("LEGO Move Hub"));
        }

        [Test]
        public void TestUnsubscribeName()
        {
            var cmd = UnsubscribeName();
            Assert.That(cmd.Message.ToArray(), Is.EquivalentTo(new byte[] {
                0x05, 0x00, 0x01, 0x01, 0x03
            }));
            var reply = cmd.ParseReplay(new byte[] {
                0x05, 0x00, 0x01, 0x01, 0x06
            });
            Assert.That(reply, Is.Null);
        }

        [Test]
        public void TestGetButtonState()
        {
            var cmd = GetButtonState();
            Assert.That(cmd.Message.ToArray(), Is.EquivalentTo(new byte[] {
                0x05, 0x00, 0x01, 0x02, 0x05
            }));

            var reply = cmd.ParseReplay(new byte[] {
                0x06, 0x00, 0x01, 0x01, 0x06, 0x00
            });
            Assert.That(reply, Is.False);

            reply = cmd.ParseReplay(new byte[] {
                0x06, 0x00, 0x01, 0x01, 0x06, 0x01
            });
            Assert.That(reply, Is.True);
        }

        [Test]
        public void TestGetFirmwareVersion()
        {
            var cmd = GetFirmwareVersion();
            Assert.That(cmd.Message.ToArray(), Is.EquivalentTo(new byte[] {
                0x05, 0x00, 0x01, 0x03, 0x05
            }));
            var reply = cmd.ParseReplay(new byte[] {
                0x09, 0x00, 0x01, 0x03, 0x06, 0x40, 0x01, 0x00, 0x10
            });
            Assert.That(reply, Is.EqualTo(new Version("1.0.00.0140")));
        }

        [Test]
        public void TestGetHardwareVersion()
        {
            var cmd = GetHardwareVersion();
            Assert.That(cmd.Message.ToArray(), Is.EquivalentTo(new byte[] {
                0x05, 0x00, 0x01, 0x04, 0x05
            }));
            var reply = cmd.ParseReplay(new byte[] {
                0x09, 0x00, 0x01, 0x04, 0x06, 0x00, 0x00, 0x00, 0x04
            });
            Assert.That(reply, Is.EqualTo(new Version("0.4.00.0000")));
        }
    }
}
