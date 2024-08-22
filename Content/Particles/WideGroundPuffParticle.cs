using Microsoft.Xna.Framework;
using Terraria;

namespace AcidicBosses.Content.Particles;

public class WideGroundPuffParticle : AnimatedParticle
{
    public WideGroundPuffParticle(Vector2 position, Vector2 velocity, float rotation, Color color, int lifetime) : base(position, velocity, rotation, color, lifetime)
    {
    }

    public override string AtlasTextureName => "AcidicBosses.GroundPlume";
    protected override int FrameWidth => 64;

    public override void FirstFrame()
    {
        Position += (7 * Scale.Y) * (Rotation - MathHelper.PiOver2).ToRotationVector2();
    }
}