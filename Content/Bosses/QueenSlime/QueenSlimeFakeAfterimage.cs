using AcidicBosses.Common;
using AcidicBosses.Core.Graphics.Sprites;
using AcidicBosses.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace AcidicBosses.Content.Bosses.QueenSlime;

public class QueenSlimeFakeAfterimage : FakeAfterimage
{
    public QueenSlimeFakeAfterimage(Vector2 startPos, Vector2 endPos, NPC toCopy, int images = 20) : base(startPos,
        endPos, toCopy, images)
    {
        var texture = TextureAssets.Npc[ToCopy.type].Value;
        var f = toCopy.GetGlobalNPC<QueenSlime>().Frame;
        var frameCount = Main.npcFrameCount[ToCopy.type];
        var frame = texture.Frame(2, 16, f / frameCount, f % frameCount);
        frame.Inflate(0, -2);
        StartFrame = frame;

        StartPos = toCopy.Bottom;
    }

    public override void Draw(SpriteBatch sb)
    {
        var progress = Utils.GetLerpValue(0, MaxTime, Time, true);
        
        var texture = TextureAssets.Npc[ToCopy.type].Value;
        var origin = StartFrame.Size() * new Vector2(0.5f, 1f);
        
        var effects = SpriteEffects.None;
        if (StartDirection < 1) effects = SpriteEffects.FlipHorizontally;
        
        sb.EnterShader();
        GameShaders.Misc["QueenSlime"].Apply();
        
        for (var i = 0; i <= Images; i++)
        {
            var fade = EasingHelper.QuadIn(1f - progress) * 0.5f * (i) / Images;
            var pos = Vector2.Lerp(StartPos, EndPos, EasingHelper.QuadIn((float) i / Images));
            
            var light = Lighting.GetColor((pos).ToTileCoordinates());
            var afterImageColor = Color.Multiply(light, fade);
            
            if (CustomDrawCode is null)
            {
                Main.spriteBatch.Draw(
                    texture, pos - Main.screenPosition,
                    StartFrame, afterImageColor,
                    StartRotation, origin, StartScale,
                    effects, 0f);
            }
            else
            {
                CustomDrawCode(Main.spriteBatch, this, pos, afterImageColor);
            }
        }
        
        sb.ExitShader();
    }
}