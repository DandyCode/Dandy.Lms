using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Dandy.Devices.USB.Libusb
{
    enum Error
    {
        Success = 0,
        IO = -1,
        InvalidParam = -2,
        Access = -3,
        NoDevice = -4,
        NotFound = -5,
        Busy = -6,
        Timeout = -7,
        Overflow = -8,
        Pipe = -9,
        Interupted = -10,
        NoMem = -11,
        NotSupported = -12,
        Other = -99,

    }

    enum Speed
    {
        Unknown,
        Low,
        Full,
        High,
        Super,
        SuperPlus,
    }

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

    struct DeviceDescriptor
    {
        #pragma warning disable CS0649
        public readonly byte bLength;
        public readonly byte bDescriptorType;
        public readonly ushort bcdUSB;
        public readonly byte bDeviceClass;
        public readonly byte bDeviceSubClass;
        public readonly byte bDeviceProtocol;
        public readonly byte bMaxPacketSize0;
        public readonly ushort idVendor;
        public readonly ushort idProduct;
        public readonly ushort bcdDevice;
        public readonly byte iManufacturer;
        public readonly byte iProduct;
        public readonly byte iSerialNumber;
        public readonly byte bNumConfigurations;
        #pragma warning restore CS0649
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

    sealed class ErrorException : Exception
    {
        public Error Error { get; }

        [DllImport("usb-1.0", CallingConvention = CallingConvention.Cdecl)]
        static extern IntPtr libusb_strerror(Error error);

        static string StrError(Error error)
        {
            var ptr = libusb_strerror(error);
            // FIXME: this is UTF-8
            return Marshal.PtrToStringAnsi(ptr);
        }

        public ErrorException(Error error) : base(StrError(error))
        {
            Error = error;
        }

        public ErrorException(int error) : this ((Error)error)
        {
        }
    }

    sealed class Context : IDisposable
    {
        bool isDefault;
        IntPtr context;

        public static Context Default { get; } = new Context(true);

        public IntPtr Handle => (context == IntPtr.Zero && !isDefault) ? throw new ObjectDisposedException(null) : context;

        [DllImport("usb-1.0", EntryPoint = "libusb_init", CallingConvention = CallingConvention.Cdecl)]
        static extern int libusb_init_default(IntPtr must_be_null);

        [DllImport("usb-1.0", CallingConvention = CallingConvention.Cdecl)]
        static extern int libusb_init(ref IntPtr context);

        Context(bool isDefault)
        {
            if (isDefault) {
                var ret = libusb_init_default(IntPtr.Zero);
                if (ret < 0) {
                    throw new ErrorException(ret);
                }
                this.isDefault = true;
            }
            else {
                var ret = libusb_init(ref context);
                if (ret < 0) {
                    throw new ErrorException(ret);
                }
            }
        }

        public Context() : this(false)
        {
        }

        ~Context()
        {
            Dispose(false);
        }

        [DllImport("usb-1.0", CallingConvention = CallingConvention.Cdecl)]
        static extern void libusb_exit(IntPtr context);

        void Dispose(bool disposing)
        {
            if (context != IntPtr.Zero || isDefault) {
                libusb_exit(context);
                context = IntPtr.Zero;
                isDefault = false;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }

    sealed class DeviceList : IDisposable, IEnumerable<Device>
    {
        IntPtr list;
        Context ctx;

        public IntPtr Handle => list == IntPtr.Zero ? throw new ObjectDisposedException(null) : list;

        [DllImport("usb-1.0", CallingConvention = CallingConvention.Cdecl)]
        static extern int libusb_get_device_list(IntPtr context, out IntPtr list);

        public DeviceList() : this(Context.Default)
        {
        }

        public DeviceList(Context ctx)
        {
            var ctx_ = ctx?.Handle ?? throw new ArgumentNullException(nameof(ctx));
            var ret = libusb_get_device_list(ctx_, out list);
            if (ret < 0) {
                throw new ErrorException(ret);
            }

            // need to keep context alive as long as device list is alive
            this.ctx = ctx;
        }

        ~DeviceList()
        {
            Dispose(false);
        }

        [DllImport("usb-1.0", CallingConvention = CallingConvention.Cdecl)]
        static extern int libusb_free_device_list(IntPtr list, int unref_devices);

        void Dispose(bool disposing)
        {
            if (list != IntPtr.Zero) {
                var ret = libusb_free_device_list(list, 1);
                if (ret < 0) {
                    // can't throw exception in finalizer
                    if (disposing) {
                        throw new ErrorException(ret);
                    }
                }
                else {
                    list = IntPtr.Zero;
                }
            }
            if (disposing) {
                ctx = null;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        IEnumerator<Device> GetEnumerator()
        {
            var offset = 0;
            IntPtr ptr;

            while ((ptr = Marshal.ReadIntPtr(Handle, offset)) != IntPtr.Zero) {
                yield return new Device(ptr, ctx);
                offset += IntPtr.Size;
            }
        }

        IEnumerator<Device> IEnumerable<Device>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    sealed class Device : IDisposable
    {
        IntPtr dev;
        Context ctx;

        public IntPtr Handle => dev == IntPtr.Zero ? throw new ObjectDisposedException(null) : dev;

        [DllImport("usb-1.0", CallingConvention = CallingConvention.Cdecl)]
        static extern byte libusb_get_bus_number(IntPtr dev);

        public byte BusNumber => libusb_get_bus_number(Handle);

        [DllImport("usb-1.0", CallingConvention = CallingConvention.Cdecl)]
        static extern byte libusb_get_port_number(IntPtr dev);

        public byte PortNumber => libusb_get_port_number(Handle);

        [DllImport("usb-1.0", CallingConvention = CallingConvention.Cdecl)]
        static extern int libusb_get_port_numbers(IntPtr dev, IntPtr port_numbers, int port_numbers_len);

        public byte[] PortNumbers {
            get {
                const int len = 7;
                var portNumbers_ = Marshal.AllocHGlobal(len);
                try {
                    var ret = libusb_get_port_numbers(Handle, portNumbers_, len);
                    if (ret < 0) {
                        throw new ErrorException(ret);
                    }
                    var portNumbers = new byte[ret];
                    Marshal.Copy(portNumbers_, portNumbers, 0, ret);
                    return portNumbers;
                }
                finally {
                    Marshal.FreeHGlobal(portNumbers_);
                }
            }
        }

        [DllImport("usb-1.0", CallingConvention = CallingConvention.Cdecl)]
        static extern IntPtr libusb_get_parent(IntPtr dev);

        public Device Parent {
            get {
                using (new DeviceList(ctx)) {
                    var parent_ = libusb_get_parent(Handle);
                    if (parent_ == IntPtr.Zero) {
                        return null;
                    }
                    return new Device(parent_, ctx);
                }
            }
        }

        [DllImport("usb-1.0", CallingConvention = CallingConvention.Cdecl)]
        static extern byte libusb_get_device_address(IntPtr dev);

        public byte Address => libusb_get_device_address(Handle);

        [DllImport("usb-1.0", CallingConvention = CallingConvention.Cdecl)]
        static extern int libusb_get_device_speed(IntPtr dev);

        public Speed Speed => (Speed)libusb_get_device_speed(Handle);

        [DllImport("usb-1.0", CallingConvention = CallingConvention.Cdecl)]
        static extern int libusb_get_max_packet_size(IntPtr dev, byte endpoint);

        public int GetMaxPacketSize(byte endpoint)
        {
            var ret = libusb_get_max_packet_size(Handle, endpoint);
            if (ret < 0) {
                throw new ErrorException(ret);
            }
            return ret;
        }

        [DllImport("usb-1.0", CallingConvention = CallingConvention.Cdecl)]
        static extern int libusb_get_max_iso_packet_size(IntPtr dev, byte endpoint);

        public int GetMaxIsoPacketSize(byte endpoint)
        {
            var ret = libusb_get_max_iso_packet_size(Handle, endpoint);
            if (ret < 0) {
                throw new ErrorException(ret);
            }
            return ret;
        }

        [DllImport("usb-1.0", CallingConvention = CallingConvention.Cdecl)]
        static extern IntPtr libusb_ref_device(IntPtr dev);

        internal Device(IntPtr dev, Context ctx)
        {
            this.dev = libusb_ref_device(dev);
            this.ctx = ctx;
        }

        ~Device()
        {
            Dispose(false);
        }

        [DllImport("usb-1.0", CallingConvention = CallingConvention.Cdecl)]
        static extern void libusb_unref_device(IntPtr dev);

        void Dispose(bool disposing)
        {
            if (dev != IntPtr.Zero) {
                libusb_unref_device(dev);
                dev = IntPtr.Zero;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        [DllImport("usb-1.0", CallingConvention = CallingConvention.Cdecl)]
        static extern int libusb_get_device_descriptor(IntPtr dev, out DeviceDescriptor desc);

        public DeviceDescriptor Descriptor {
            get {
                var ret = libusb_get_device_descriptor(Handle, out var descriptor);
                if (ret < 0) {
                    throw new ErrorException(ret);
                }
                return descriptor;
            }
        }

        public DeviceHandle Open()
        {
            return new DeviceHandle(this, ctx);
        }
    }

    sealed class DeviceHandle : IDisposable
    {
        IntPtr devHandle;
        Context ctx;

        public IntPtr Handle => devHandle == IntPtr.Zero ? throw new ObjectDisposedException(null) : devHandle;

        [DllImport("usb-1.0", CallingConvention = CallingConvention.Cdecl)]
        static extern int libusb_open(IntPtr dev, out IntPtr dev_handle);

        internal DeviceHandle(Device dev, Context ctx)
        {
            var dev_ = dev?.Handle ?? throw new ArgumentNullException(nameof(dev));
            var ret = libusb_open(dev_, out devHandle);
            if (ret < 0) {
                throw new ErrorException(ret);
            }
            this.ctx = ctx;
        }

        [DllImport("usb-1.0", CallingConvention = CallingConvention.Cdecl)]
        static extern IntPtr libusb_open_device_with_vid_pid(IntPtr ctx, ushort vendor_id, ushort product_id);

        public DeviceHandle(Context ctx, ushort vendorId, ushort productId)
        {
            var ctx_ = ctx?.Handle ?? throw new ArgumentNullException(nameof(ctx));
            devHandle = libusb_open_device_with_vid_pid(ctx_, vendorId, productId);
            if (devHandle == IntPtr.Zero) {
                // REVISIT: could be other error as well
                throw new ErrorException(Error.NotFound);
            }
            this.ctx = ctx;
        }

        ~DeviceHandle()
        {
            Dispose(false);
        }

        [DllImport("usb-1.0", CallingConvention = CallingConvention.Cdecl)]
        static extern void libusb_close(IntPtr list);

        void Dispose(bool disposing)
        {
            if (devHandle != IntPtr.Zero) {
                libusb_close(devHandle);
                devHandle = IntPtr.Zero;
            }
            if (disposing) {
                ctx = null;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        [DllImport("usb-1.0", CallingConvention = CallingConvention.Cdecl)]
        static extern IntPtr libusb_get_device(IntPtr dev_handle);

        public Device Device => new Device(libusb_get_device(Handle), ctx);

        [DllImport("usb-1.0", CallingConvention = CallingConvention.Cdecl)]
        static extern int libusb_get_configuration(IntPtr dev_handle, out int config);

        [DllImport("usb-1.0", CallingConvention = CallingConvention.Cdecl)]
        static extern int libusb_set_configuration(IntPtr dev_handle, int config);

        public int Configuration {
            get {
                var ret = libusb_get_configuration(Handle, out var config);
                if (ret < 0) {
                    throw new ErrorException(ret);
                }
                return config;
            }
            set {
                var ret = libusb_set_configuration(Handle, value);
                if (ret < 0) {
                    throw new ErrorException(ret);
                }
            }
        }

        [DllImport("usb-1.0", CallingConvention = CallingConvention.Cdecl)]
        static extern int libusb_claim_interface(IntPtr dev_handle, int interface_number);

        public void ClaimInterface(int interfaceNumber)
        {
            var ret = libusb_claim_interface(Handle, interfaceNumber);
            if (ret < 0) {
                throw new ErrorException(ret);
            }
        }

        [DllImport("usb-1.0", CallingConvention = CallingConvention.Cdecl)]
        static extern int libusb_release_interface(IntPtr dev_handle, int interface_number);

        public void ReleaseInterface(int interfaceNumber)
        {
            var ret = libusb_release_interface(Handle, interfaceNumber);
            if (ret < 0) {
                throw new ErrorException(ret);
            }
        }

        [DllImport("usb-1.0", CallingConvention = CallingConvention.Cdecl)]
        static extern int libusb_set_interface_alt_setting(IntPtr dev_handle, int interface_number, int alternate_setting);

        public void SetInterfaceAlternateSetting(int interfaceNumber, int alternateSetting)
        {
            var ret = libusb_set_interface_alt_setting(Handle, interfaceNumber, alternateSetting);
            if (ret < 0) {
                throw new ErrorException(ret);
            }
        }

        [DllImport("usb-1.0", CallingConvention = CallingConvention.Cdecl)]
        static extern int libusb_clear_halt(IntPtr dev_handle,  byte endpoint);

        public void ClearHalt(byte endpoint)
        {
            var ret = libusb_clear_halt(Handle, endpoint);
            if (ret < 0) {
                throw new ErrorException(ret);
            }
        }

        [DllImport("usb-1.0", CallingConvention = CallingConvention.Cdecl)]
        static extern int libusb_reset_device(IntPtr dev_handle);

        public void ResetDevice(byte endpoint)
        {
            var ret = libusb_reset_device(Handle);
            if (ret < 0) {
                throw new ErrorException(ret);
            }
        }

        [DllImport("usb-1.0", CallingConvention = CallingConvention.Cdecl)]
        static extern int libusb_kernel_driver_active(IntPtr dev_handle, int interface_number);

        public bool IsKernelDriverActive(int interfaceNumber)
        {
            var ret = libusb_kernel_driver_active(Handle, interfaceNumber);
            if (ret < 0) {
                throw new ErrorException(ret);
            }
            return Convert.ToBoolean(ret);
        }

        [DllImport("usb-1.0", CallingConvention = CallingConvention.Cdecl)]
        static extern int libusb_detach_kernel_driver(IntPtr dev_handle, int interface_number);

        public void DetachKernelDriver(int interfaceNumber)
        {
            var ret = libusb_detach_kernel_driver(Handle, interfaceNumber);
            if (ret < 0) {
                throw new ErrorException(ret);
            }
        }

        [DllImport("usb-1.0", CallingConvention = CallingConvention.Cdecl)]
        static extern int libusb_attach_kernel_driver(IntPtr dev_handle, int interface_number);

        public void AttachKernelDriver(int interfaceNumber)
        {
            var ret = libusb_attach_kernel_driver(Handle, interfaceNumber);
            if (ret < 0) {
                throw new ErrorException(ret);
            }
        }

        [DllImport("usb-1.0", CallingConvention = CallingConvention.Cdecl)]
        static extern int libusb_set_auto_detach_kernel_driver(IntPtr dev_handle, int enable);

        public void SetAutoDetachKernelDriver(bool enable)
        {
            var ret = libusb_set_auto_detach_kernel_driver(Handle, enable ? 1 : 0);
            if (ret < 0) {
                throw new ErrorException(ret);
            }
        }

        [DllImport("usb-1.0", CallingConvention = CallingConvention.Cdecl)]
        static extern int libusb_control_transfer(IntPtr dev_handle, byte bmRequestType, byte bRequest, ushort wValue, ushort wIndex, IntPtr data, ushort wLength, uint timeout);

        public void ControlTransfer(byte bmRequestType, byte bRequest, ushort wValue, ushort wIndex, byte[] data, ushort wLength, uint timeout = 0)
        {
            if (data == null) {
                throw new ArgumentNullException(nameof(data));
            }

            if (wLength > data.Length) {
                throw new ArgumentOutOfRangeException(nameof(wLength));
            }

            var gcHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
            try {
                var data_ = gcHandle.AddrOfPinnedObject();
                var ret = libusb_control_transfer(Handle, bmRequestType, bRequest, wValue, wIndex, data_, wLength, timeout);
                if (ret < 0) {
                    throw new ErrorException(ret);
                }
            }
            finally {
                gcHandle.Free();
            }
        }

        [DllImport("usb-1.0", CallingConvention = CallingConvention.Cdecl)]
        static extern int libusb_bulk_transfer(IntPtr dev_handle, byte endpoint, IntPtr data, int length, out int transferred, uint timeout);

        public int BulkTransfer(byte endpoint, byte[] data, uint timeout = 0)
        {
            if (data == null) {
                throw new ArgumentNullException(nameof(data));
            }

            var gcHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
            try {
                var data_ = gcHandle.AddrOfPinnedObject();
                var ret = libusb_bulk_transfer(Handle, endpoint, data_, data.Length, out var transferred, timeout);
                if (ret < 0) {
                    throw new ErrorException(ret);
                }
                return transferred;
            }
            finally {
                gcHandle.Free();
            }
        }

        [DllImport("usb-1.0", CallingConvention = CallingConvention.Cdecl)]
        static extern int libusb_interrupt_transfer(IntPtr dev_handle, byte endpoint, IntPtr data, int length, out int transferred, uint timeout);

        public int InterruptTransfer(byte endpoint, byte[] data, uint timeout = 0)
        {
            if (data == null) {
                throw new ArgumentNullException(nameof(data));
            }

            var gcHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
            try {
                var data_ = gcHandle.AddrOfPinnedObject();
                var ret = libusb_interrupt_transfer(Handle, endpoint, data_, data.Length, out var transferred, timeout);
                if (ret < 0) {
                    throw new ErrorException(ret);
                }
                return transferred;
            }
            finally {
                gcHandle.Free();
            }
        }
    }
}
