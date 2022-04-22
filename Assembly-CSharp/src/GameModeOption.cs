using System;

namespace AssemblyCSharp
{
    [Flags]
    public enum GameModeOption
    {
        None = 0x0,
        Permadeath = 0x1,
        NoSurvival = 0x2,
        NoCost = 0x4,
        NoBlueprints = 0x8,
        NoEnergy = 0x10,
        NoPressure = 0x20,
        NoOxygen = 0x40,
        NoAggression = 0x80,
        NoHints = 0x100,
        NoRadiation = 0x200,
        InitialItems = 0x400,
        Cheats = 0x6FC,
        Survival = 0x0,
        Hardcore = 0x101,
        Freedom = 0x2,
        Creative = 0x6FE
    }
}
