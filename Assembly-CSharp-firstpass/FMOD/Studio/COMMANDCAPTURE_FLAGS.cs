using System;

namespace FMOD.Studio
{
    [Flags]
    public enum COMMANDCAPTURE_FLAGS : uint
    {
        NORMAL = 0x0u,
        FILEFLUSH = 0x1u,
        SKIP_INITIAL_STATE = 0x2u
    }
}
