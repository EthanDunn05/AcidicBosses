using AcidicBosses.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;

namespace AcidicBosses.Core.Graphics.Sprites;

public class FadingEffectLine : EffectLine
{
    private Color desiredColor;
    public FadingEffectLine(Asset<Texture2D> texture, Vector2 position, float rotation, float length, float width, Color color, int lifetime) : base(texture, position, rotation, length, width, color, lifetime)
    {
        desiredColor = DrawColor;
    }

    public override void Update()
    {
        base.Update();
        DrawColor = desiredColor * EasingHelper.CubicOut(1f - LifetimeRatio);
    }
}