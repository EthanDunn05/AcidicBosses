using Microsoft.Xna.Framework;

namespace AcidicBosses.Content.Particles.Animated;

public class SmallBeeParticle : AnimatedParticle
{
    public SmallBeeParticle(Vector2 position, Vector2 velocity, float rotation, Color color, int lifetime) : base(position, velocity, rotation, color, lifetime)
    {
        Scale = Vector2.One;
        Looping = true;
    }

    public override string AtlasTextureName => "AcidicBosses.SmallBee";
    protected override int FrameWidth => 10;
    protected override int FrameHeight => 12;
}