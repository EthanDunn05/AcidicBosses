using Microsoft.Xna.Framework;

namespace AcidicBosses.Content.Particles;

public class SmallPuffParticle : AnimatedParticle
{
    public SmallPuffParticle(Vector2 position, Vector2 velocity, float rotation, Color color, int lifetime) : base(position, velocity, rotation, color, lifetime)
    {
    }

    public override string AtlasTextureName => "AcidicBosses.SmallPuff";
    protected override int FrameWidth => 64;
}