using AcidicBosses.Common.Textures;
using Microsoft.Xna.Framework;
using Terraria;

namespace AcidicBosses.Content.Particles;

public class SmokeRingParticle : AnimatedParticle
{
    public override string AtlasTextureName => "AcidicBosses.GroundRing";
    protected override int FrameWidth => 64;

    public SmokeRingParticle(Vector2 position, Vector2 velocity, float rotation, Color color, int lifetime) : base(position, velocity, rotation, color, lifetime)
    {
    }
}