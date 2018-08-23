namespace Dandy.Lms.Nxt.Commands
{
    enum SystemCommandCode : byte
    {
        OpenRead = 0x80,
        OpenWrite = 0x81,
        Read = 0x82,
        Write = 0x83,
        Close = 0x84,
        Delete = 0x85,
        FindFirst = 0x86,
        FindNext = 0x87,
        GetFirmwareVersion = 0x88,
        OpenWriteLinear = 0x89,
        OpenReadLinear = 0x8A,
        OpenWriteData = 0x8B,
        OpenAppendData = 0x8C,
        Boot = 0x97,
        SetBrickName = 0x98,
        GetDeviceInfo = 0x9B,
    }
}
