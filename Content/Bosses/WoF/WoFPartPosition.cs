using System;

namespace AcidicBosses.Content.Bosses.WoF;

[Flags]
public enum WoFPartPosition
{
    Right = 1 << 1,
    Left = 1 << 2,
    Top = 1 << 3,
    Center = 1 << 4,
    Bottom = 1 << 5
}