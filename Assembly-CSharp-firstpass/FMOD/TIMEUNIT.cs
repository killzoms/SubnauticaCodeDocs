using System;

namespace FMOD
{
    [Flags]
    public enum TIMEUNIT : uint
    {
        MS = 0x1u,
        PCM = 0x2u,
        PCMBYTES = 0x4u,
        RAWBYTES = 0x8u,
        PCMFRACTION = 0x10u,
        MODORDER = 0x100u,
        MODROW = 0x200u,
        MODPATTERN = 0x400u
    }
}
