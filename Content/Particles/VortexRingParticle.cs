using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace AcidicBosses.Content.Particles;

public class VortexRingParticle : BetterParticle
{
    public VortexRingParticle(Vector2 position, Vector2 velocity, float rotation, Color color, int lifetime) : base(position, velocity, rotation, color, lifetime)
    {
    }

    public override string AtlasTextureName => "AcidicBosses.VortexRing";
    
    public override BlendState BlendState => BlendState.Additive;
}