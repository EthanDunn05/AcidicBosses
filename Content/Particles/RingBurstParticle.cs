using Microsoft.Xna.Framework;

namespace AcidicBosses.Content.Particles;

public class RingBurstParticle : AnimatedParticle
{
    public RingBurstParticle(Vector2 position, Vector2 velocity, float rotation, Color color, int lifetime) : base(position, velocity, rotation, color, lifetime)
    {
    }

    public override string AtlasTextureName => "AcidicBosses.Ring";
    protected override int FrameWidth => 64;
}