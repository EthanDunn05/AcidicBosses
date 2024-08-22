using Microsoft.Xna.Framework;
using Terraria;

namespace AcidicBosses.Content.Particles;

public class FireSmokeParticle : AnimatedParticle
{
    public FireSmokeParticle(Vector2 position, Vector2 velocity, float rotation, Color color, int lifetime) : base(position, velocity, rotation, color, lifetime)
    {
    }
    
    

    public override string AtlasTextureName => "AcidicBosses.FireSmoke";
    protected override int FrameWidth => 64;
    public override void FirstFrame()
    {
        Position += (23 * Scale.Y) * (Rotation - MathHelper.PiOver2).ToRotationVector2();
    }
}