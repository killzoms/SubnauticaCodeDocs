using System;

namespace FMOD
{
    [Flags]
    public enum MEMORY_TYPE : uint
    {
        NORMAL = 0x0u,
        STREAM_FILE = 0x1u,
        STREAM_DECODE = 0x2u,
        SAMPLEDATA = 0x4u,
        DSP_BUFFER = 0x8u,
        PLUGIN = 0x10u,
        XBOX360_PHYSICAL = 0x100000u,
        PERSISTENT = 0x200000u,
        SECONDARY = 0x400000u,
        ALL = uint.MaxValue
    }
}
