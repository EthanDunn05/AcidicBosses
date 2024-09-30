using Microsoft.Xna.Framework;

namespace AcidicBosses.Content.Particles.Animated;

public class CrosshairBurstParticle : AnimatedParticle
{
    public CrosshairBurstParticle(Vector2 position, Vector2 velocity, float rotation, Color color, int lifetime) : base(position, velocity, rotation, color, lifetime)
    {
    }

    public override string AtlasTextureName => "AcidicBosses.CrosshairBurst";
    protected override int FrameWidth => 64;
}