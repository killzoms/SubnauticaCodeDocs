using System;

namespace FMOD.Studio
{
    [Flags]
    public enum SYSTEM_CALLBACK_TYPE : uint
    {
        PREUPDATE = 0x1u,
        POSTUPDATE = 0x2u,
        BANK_UNLOAD = 0x4u,
        ALL = uint.MaxValue
    }
}
