using System;

namespace FMOD.Studio
{
    [Flags]
    public enum LOAD_BANK_FLAGS : uint
    {
        NORMAL = 0x0u,
        NONBLOCKING = 0x1u,
        DECOMPRESS_SAMPLES = 0x2u
    }
}
