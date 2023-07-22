using Microsoft.Xna.Framework.Graphics;

namespace AcidicBosses.Common.Primitive;

public interface IPrimDrawer
{
    public bool drawBehindNpcs { get; }
    public void DrawPrims(SpriteBatch spriteBatch);
}