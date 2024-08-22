using Microsoft.Xna.Framework;

namespace AcidicBosses.Content.Particles;

public class GatherEnergyParticle : AnimatedParticle
{
    public GatherEnergyParticle(Vector2 position, Vector2 velocity, float rotation, Color color, int lifetime) : base(position, velocity, rotation, color, lifetime)
    {
    }

    public override string AtlasTextureName => "AcidicBosses.GatherEnergy";
    protected override int FrameWidth => 64;
}