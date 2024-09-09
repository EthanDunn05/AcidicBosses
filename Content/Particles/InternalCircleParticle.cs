using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace AcidicBosses.Content.Particles;

public class InternalCircleParticle : BetterParticle
{
    public InternalCircleParticle(Vector2 position, Vector2 velocity, float rotation, Color color, int lifetime) : base(position, velocity, rotation, color, lifetime)
    {
    }

    public override string AtlasTextureName => "AcidicBosses.InternalFadeCircle";
    public override BlendState BlendState => BlendState.Additive;
}