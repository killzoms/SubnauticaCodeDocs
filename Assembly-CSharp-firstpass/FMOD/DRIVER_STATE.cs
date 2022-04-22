using System;

namespace FMOD
{
    [Flags]
    public enum DRIVER_STATE : uint
    {
        CONNECTED = 0x1u,
        DEFAULT = 0x2u
    }
}
