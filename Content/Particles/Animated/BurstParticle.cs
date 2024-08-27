using Microsoft.Xna.Framework;

namespace AcidicBosses.Content.Particles.Animated;

public class BurstParticle : AnimatedParticle
{
    public BurstParticle(Vector2 position, Vector2 velocity, float rotation, Color color, int lifetime) : base(position, velocity, rotation, color, lifetime)
    {
    }

    public override string AtlasTextureName => "AcidicBosses.Burst";
    protected override int FrameWidth => 64;
}