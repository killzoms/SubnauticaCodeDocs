using System;

namespace FMOD
{
    [Flags]
    public enum DEBUG_FLAGS : uint
    {
        NONE = 0x0u,
        ERROR = 0x1u,
        WARNING = 0x2u,
        LOG = 0x4u,
        TYPE_MEMORY = 0x100u,
        TYPE_FILE = 0x200u,
        TYPE_CODEC = 0x400u,
        TYPE_TRACE = 0x800u,
        DISPLAY_TIMESTAMPS = 0x10000u,
        DISPLAY_LINENUMBERS = 0x20000u,
        DISPLAY_THREAD = 0x40000u
    }
}
