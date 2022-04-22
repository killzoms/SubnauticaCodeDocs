using System;

namespace FMOD
{
    [Flags]
    public enum SYSTEM_CALLBACK_TYPE : uint
    {
        DEVICELISTCHANGED = 0x1u,
        DEVICELOST = 0x2u,
        MEMORYALLOCATIONFAILED = 0x4u,
        THREADCREATED = 0x8u,
        BADDSPCONNECTION = 0x10u,
        PREMIX = 0x20u,
        POSTMIX = 0x40u,
        ERROR = 0x80u,
        MIDMIX = 0x100u,
        THREADDESTROYED = 0x200u,
        PREUPDATE = 0x400u,
        POSTUPDATE = 0x800u,
        RECORDLISTCHANGED = 0x1000u,
        ALL = uint.MaxValue
    }
}
