using System;

namespace FMOD
{
    [Flags]
    public enum MODE : uint
    {
        DEFAULT = 0x0u,
        LOOP_OFF = 0x1u,
        LOOP_NORMAL = 0x2u,
        LOOP_BIDI = 0x4u,
        _2D = 0x8u,
        _3D = 0x10u,
        CREATESTREAM = 0x80u,
        CREATESAMPLE = 0x100u,
        CREATECOMPRESSEDSAMPLE = 0x200u,
        OPENUSER = 0x400u,
        OPENMEMORY = 0x800u,
        OPENMEMORY_POINT = 0x10000000u,
        OPENRAW = 0x1000u,
        OPENONLY = 0x2000u,
        ACCURATETIME = 0x4000u,
        MPEGSEARCH = 0x8000u,
        NONBLOCKING = 0x10000u,
        UNIQUE = 0x20000u,
        _3D_HEADRELATIVE = 0x40000u,
        _3D_WORLDRELATIVE = 0x80000u,
        _3D_INVERSEROLLOFF = 0x100000u,
        _3D_LINEARROLLOFF = 0x200000u,
        _3D_LINEARSQUAREROLLOFF = 0x400000u,
        _3D_INVERSETAPEREDROLLOFF = 0x800000u,
        _3D_CUSTOMROLLOFF = 0x4000000u,
        _3D_IGNOREGEOMETRY = 0x40000000u,
        IGNORETAGS = 0x2000000u,
        LOWMEM = 0x8000000u,
        LOADSECONDARYRAM = 0x20000000u,
        VIRTUAL_PLAYFROMSTART = 0x80000000u
    }
}
