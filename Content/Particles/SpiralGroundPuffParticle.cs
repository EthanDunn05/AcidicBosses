using Microsoft.Xna.Framework;
using Terraria;

namespace AcidicBosses.Content.Particles;

public class SpiralGroundPuffParticle : AnimatedParticle
{
    public SpiralGroundPuffParticle(Vector2 position, Vector2 velocity, float rotation, Color color, int lifetime) : base(position, velocity, rotation, color, lifetime)
    {
    }

    public override string AtlasTextureName => "AcidicBosses.GroundPuff";
    protected override int FrameWidth => 64;
    
    public override void FirstFrame()
    {
        Position += (17 * Scale.Y) * (Rotation - MathHelper.PiOver2).ToRotationVector2();
    }
}