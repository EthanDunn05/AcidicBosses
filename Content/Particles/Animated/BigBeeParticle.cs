using Microsoft.Xna.Framework;

namespace AcidicBosses.Content.Particles.Animated;

public class BigBeeParticle : AnimatedParticle
{
    public BigBeeParticle(Vector2 position, Vector2 velocity, float rotation, Color color, int lifetime) : base(position, velocity, rotation, color, lifetime)
    {
        Scale = Vector2.One;
        Looping = true;
    }

    public override string AtlasTextureName => "AcidicBosses.BigBee";
    protected override int FrameWidth => 16;
    protected override int FrameHeight => 18;
}