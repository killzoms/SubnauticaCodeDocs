using System;

namespace FMOD.Studio
{
    [Flags]
    public enum COMMANDREPLAY_FLAGS : uint
    {
        NORMAL = 0x0u,
        SKIP_CLEANUP = 0x1u,
        FAST_FORWARD = 0x2u,
        SKIP_BANK_LOAD = 0x4u
    }
}
