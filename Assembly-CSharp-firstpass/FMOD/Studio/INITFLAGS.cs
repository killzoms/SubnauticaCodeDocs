using System;

namespace FMOD.Studio
{
    [Flags]
    public enum INITFLAGS : uint
    {
        NORMAL = 0x0u,
        LIVEUPDATE = 0x1u,
        ALLOW_MISSING_PLUGINS = 0x2u,
        SYNCHRONOUS_UPDATE = 0x4u,
        DEFERRED_CALLBACKS = 0x8u,
        LOAD_FROM_UPDATE = 0x10u
    }
}
