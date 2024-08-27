using Microsoft.Xna.Framework;

namespace AcidicBosses.Content.Particles;

public class SharpTearParticle : BetterParticle
{
    public SharpTearParticle(Vector2 position, Vector2 velocity, float rotation, Color color, int lifetime) : base(position, velocity, rotation, color, lifetime)
    {
    }

    public override string AtlasTextureName => "AcidicBosses.SharpTear";
}