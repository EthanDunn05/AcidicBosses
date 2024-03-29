﻿using AcidicBosses.Common.Textures;
using AcidicBosses.Content.ProjectileBases;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;

namespace AcidicBosses.Content.Bosses.BoC;

public class CreeperDashLine : BaseLineProjectile
{
    protected override float Length { get; } = 12000f;
    protected override float Width { get; } = 14;
    protected override Color Color { get; } = Color.Crimson;
    protected override Asset<Texture2D> LineTexture { get; } = TextureRegistry.InvertedGlowLine;
}