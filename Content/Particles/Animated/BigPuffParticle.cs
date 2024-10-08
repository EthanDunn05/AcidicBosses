﻿using Microsoft.Xna.Framework;

namespace AcidicBosses.Content.Particles.Animated;

public class BigPuffParticle : AnimatedParticle
{
    public BigPuffParticle(Vector2 position, Vector2 velocity, float rotation, Color color, int lifetime) : base(position, velocity, rotation, color, lifetime)
    {
    }

    public override string AtlasTextureName => "AcidicBosses.Simple";
    protected override int FrameWidth => 64;
}