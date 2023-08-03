﻿using AcidicBosses.Content.ProjectileBases;
using Microsoft.Xna.Framework;

namespace AcidicBosses.Content.Bosses.EoC;

public class EyeDashLine : DashLineBase
{
    protected override float Length { get; } = 12000f;
    protected override float Width { get; } = 35f;
    protected override Color Color { get; } = Color.Crimson;
}