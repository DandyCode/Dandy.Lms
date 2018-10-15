using System;
using Dandy.Devices.BluetoothLE;
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
        public void TestResetName()
        {
            var cmd = ResetName();
            Assert.That(cmd.Message.ToArray(), Is.EquivalentTo(new byte[] {
                0x05, 0x00, 0x01, 0x01, 0x04
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
            Assert.That(() => {
                var reply = cmd.ParseReplay(new byte[] {
                    0x05, 0x00, 0x01, 0x01, 0x06
                });
            }, Throws.InvalidOperationException);
        }

        [Test]
        public void TestGetButtonState()
        {
            var cmd = GetButtonState();
            Assert.That(cmd.Message.ToArray(), Is.EquivalentTo(new byte[] {
                0x05, 0x00, 0x01, 0x02, 0x05
            }));

            var reply = cmd.ParseReplay(new byte[] {
                0x06, 0x00, 0x01, 0x02, 0x06, 0x00
            });
            Assert.That(reply, Is.False);

            reply = cmd.ParseReplay(new byte[] {
                0x06, 0x00, 0x01, 0x02, 0x06, 0x01
            });
            Assert.That(reply, Is.True);
        }

        [Test]
        public void TestSubscribeButtonState()
        {
            var cmd = SubscribeButtonState();
            Assert.That(cmd.Message.ToArray(), Is.EquivalentTo(new byte[] {
                0x05, 0x00, 0x01, 0x02, 0x02
            }));
            var reply = cmd.ParseReplay(new byte[] {
                0x06, 0x00, 0x01, 0x02, 0x06, 0x00
            });
            Assert.That(reply, Is.False);
        }

        [Test]
        public void TestUnsubscribeButtonState()
        {
            var cmd = UnsubscribeButtonState();
            Assert.That(cmd.Message.ToArray(), Is.EquivalentTo(new byte[] {
                0x05, 0x00, 0x01, 0x02, 0x03
            }));
            Assert.That(() => {
                var reply = cmd.ParseReplay(new byte[] {
                    0x06, 0x00, 0x01, 0x02, 0x06, 0x00
                });
            }, Throws.InvalidOperationException);
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
        
        [Test]
        public void TestGetRSSI()
        {
            var cmd = GetRSSI();
            Assert.That(cmd.Message.ToArray(), Is.EquivalentTo(new byte[] {
                0x05, 0x00, 0x01, 0x05, 0x05
            }));

            var reply = cmd.ParseReplay(new byte[] {
                0x06, 0x00, 0x01, 0x05, 0x06, 0xd3
            });
            Assert.That(reply, Is.EqualTo(-45));
        }

        [Test]
        public void TestSubscribeRSSI()
        {
            var cmd = SubscribeRSSI();
            Assert.That(cmd.Message.ToArray(), Is.EquivalentTo(new byte[] {
                0x05, 0x00, 0x01, 0x05, 0x02
            }));
            var reply = cmd.ParseReplay(new byte[] {
                0x06, 0x00, 0x01, 0x05, 0x06, 0xd3
            });
            Assert.That(reply, Is.EqualTo(-45));
        }

        [Test]
        public void TestUnsubscribeRSSI()
        {
            var cmd = UnsubscribeRSSI();
            Assert.That(cmd.Message.ToArray(), Is.EquivalentTo(new byte[] {
                0x05, 0x00, 0x01, 0x05, 0x03
            }));
            Assert.That(() => {
                var reply = cmd.ParseReplay(new byte[] {
                    0x06, 0x00, 0x01, 0x05, 0x06, 0x00
                });
            }, Throws.InvalidOperationException);
        }

        [Test]
        public void TestGetBatteryPercent()
        {
            var cmd = GetBatteryPercent();
            Assert.That(cmd.Message.ToArray(), Is.EquivalentTo(new byte[] {
                0x05, 0x00, 0x01, 0x06, 0x05
            }));

            var reply = cmd.ParseReplay(new byte[] {
                0x06, 0x00, 0x01, 0x06, 0x06, 0x64
            });
            Assert.That(reply, Is.EqualTo(100));
        }

        [Test]
        public void TestGetManufacturer()
        {
            var cmd = GetManufacturer();
            Assert.That(cmd.Message.ToArray(), Is.EquivalentTo(new byte[] {
                0x05, 0x00, 0x01, 0x08, 0x05
            }));

            var reply = cmd.ParseReplay(new byte[] {
                0x14, 0x00, 0x01, 0x08, 0x06, 0x4c, 0x45, 0x47, 0x4f, 0x20,
                0x53, 0x79, 0x73, 0x74, 0x65, 0x6d, 0x20, 0x41, 0x2f, 0x53
            });
            Assert.That(reply, Is.EqualTo("LEGO System A/S"));
        }

        [Test]
        public void TestGetBluetoothFirmwareVersion()
        {
            var cmd = GetBluetoothFirmwareVersion();
            Assert.That(cmd.Message.ToArray(), Is.EquivalentTo(new byte[] {
                0x05, 0x00, 0x01, 0x09, 0x05
            }));

            var reply = cmd.ParseReplay(new byte[] {
                0x09, 0x00, 0x01, 0x09, 0x06, 0x37, 0x2e, 0x32, 0x63
            });
            Assert.That(reply, Is.EqualTo("7.2c"));
        }

        [Test]
        public void TestGetHubType()
        {
            var cmd = GetHubType();
            Assert.That(cmd.Message.ToArray(), Is.EquivalentTo(new byte[] {
                0x05, 0x00, 0x01, 0x0b, 0x05
            }));

            var reply = cmd.ParseReplay(new byte[] {
                0x06, 0x00, 0x01, 0x0b, 0x06, 0x40
            });
            Assert.That(reply, Is.EqualTo(HubType.MoveHub));
        }

        [Test]
        public void TestGetBluetoothAddress()
        {
            var cmd = GetBluetoothAddress();
            Assert.That(cmd.Message.ToArray(), Is.EquivalentTo(new byte[] {
                0x05, 0x00, 0x01, 0x0d, 0x05
            }));

            var reply = cmd.ParseReplay(new byte[] {
                0x0b, 0x00, 0x01, 0x0d, 0x06, 0x00, 0x16, 0x53, 0x01, 0x23, 0x45
            });
            Assert.That(reply, Is.EqualTo(BluetoothAddress.Parse("00:16:53:01:23:45")));
        }

        [Test]
        public void TestGetBootloaderBluetoothAddress()
        {
            var cmd = GetBootloaderBluetoothAddress();
            Assert.That(cmd.Message.ToArray(), Is.EquivalentTo(new byte[] {
                0x05, 0x00, 0x01, 0x0e, 0x05
            }));

            var reply = cmd.ParseReplay(new byte[] {
                0x0b, 0x00, 0x01, 0x0e, 0x06, 0x00, 0x16, 0x53, 0x01, 0x23, 0x45
            });
            Assert.That(reply, Is.EqualTo(BluetoothAddress.Parse("00:16:53:01:23:45")));
        }
    }
}
