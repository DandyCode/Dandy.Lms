using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Dandy.Devices.USB.Libusb
{
    enum ClassCode : byte
    {
        PerInterface = 0,
        Audio = 1,
        Comm = 2,
        HID = 3,
        Physical = 5,
        Printer = 7,
        PTP = 6,
        Image = 6,
        MassStorage = 8,
        Hub = 9,
        Data = 10,
        SmartCard = 0x0b,
        ContentSecurity = 0x0d,
        Video = 0x0e,
        PersonalHealthcare = 0x0f,
        DiagnosticDevice = 0xdc,
        Wireless = 0xe0,
        Application = 0xfe,
        VendorSpec = 0xff
    }

    enum DescriptorType : byte
    {
        Device = 0x01,
        Config = 0x02,
        String = 0x03,
        Interface = 0x04,
        Endpoint = 0x05,
        BOS = 0x0f,
        Capability = 0x10,
        HID = 0x21,
        Report = 0x22,
        Physical = 0x23,
        Hub = 0x29,
        SuperspeedHub = 0x2a,
        SSEndpointCompanion = 0x30,
    }

    enum EndpointDirection : byte
    {
        In = 0x80,
        Out = 0x00,
    }

    enum TransferType
    {
        Control,
        Isochronous,
        Bulk,
        Interrupt,
        BulkStream,
    }

    enum IsoSyncType
    {
        None,
        Async,
        Adaptive,
        Sync,
    }

    enum IsoUsageType
    {
        Data,
        Feedback,
        Implicit,
    }

    struct EndpointDescriptor
    {
        #pragma warning disable CS0649
        public readonly byte bLength;
        public readonly byte bDescriptorType;
        public readonly byte bEndpointAddress;
        public readonly byte bmAttributes;
        public readonly ushort wMaxPacketSize;
        public readonly byte bInterval;
        public readonly byte bRefresh;
        public readonly byte bSynchAddress;
        readonly IntPtr extra;
        readonly int extra_length;
        #pragma warning restore CS0649
        public byte[] Extra {
            get {
                var extra = new byte[extra_length];
                Marshal.Copy(this.extra, extra, 0, extra_length);
                return extra;
            }
        }
    }

    struct InterfaceDescriptor
    {
        #pragma warning disable CS0649
        public readonly byte bLength;
        public readonly byte bDescriptorType;
        public readonly byte bInterfaceNumber;
        public readonly byte bAlternateSetting;
        public readonly byte bNumEndpoints;
        public readonly byte bInterfaceClass;
        public readonly byte bInterfaceSubClass;
        public readonly byte bInterfaceProtocol;
        public readonly byte iInterface;
        readonly IntPtr endpoint;
        readonly IntPtr extra;
        readonly int extra_length;
        #pragma warning restore CS0649
        public EndpointDescriptor Endpoint => Marshal.PtrToStructure<EndpointDescriptor>(endpoint);
        public byte[] Extra {
            get {
                var extra = new byte[extra_length];
                Marshal.Copy(this.extra, extra, 0, extra_length);
                return extra;
            }
        }
    }

    struct Interface
    {
        #pragma warning disable CS0649
        readonly IntPtr altsetting;
        readonly int num_altsetting;
        #pragma warning restore CS0649

        public InterfaceDescriptor this[int index] {
            get {
                if (index < 0 || index >= num_altsetting) {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }
                return Marshal.PtrToStructure<InterfaceDescriptor>(altsetting + IntPtr.Size * index);
            }
        }
    }
}
