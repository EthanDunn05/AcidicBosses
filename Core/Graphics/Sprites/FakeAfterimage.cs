using System;
using System.Linq;
using AcidicBosses.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;

namespace AcidicBosses.Core.Graphics.Sprites;

public class FakeAfterimage
{
    public delegate void CustomDraw(SpriteBatch sb, FakeAfterimage afterimage, Vector2 oldPos, Color fadedColor);
    
    public Vector2 StartPos;
    public Vector2 EndPos;
    public int Images;
    public NPC ToCopy;
    public int Time = 0;
    
    public Rectangle StartFrame;
    public float StartRotation;
    public int StartDirection;
    public float StartScale;

    public CustomDraw? CustomDrawCode = null;
    
    public int MaxTime => Images;

    public FakeAfterimage(Vector2 startPos, Vector2 endPos, NPC toCopy, int images = 20)
    {
        StartPos = startPos;
        EndPos = endPos;
        Images = images;
        ToCopy = toCopy;

        StartFrame = toCopy.frame;
        StartRotation = toCopy.rotation;
        StartDirection = toCopy.spriteDirection;
        StartScale = toCopy.scale;
    }

    public FakeAfterimage Spawn()
    {
        if (!AcidUtils.IsClient()) return this;

        Time = 0;

        if (SpriteSystem.Afterimages.Count > 100)
        {
            SpriteSystem.Afterimages.First().Kill();
        }
        
        SpriteSystem.Afterimages.Add(this);
        return this;
    }
    
    public void Kill() => Time = MaxTime;

    public virtual void Update()
    {
        Time++;
    }

    public virtual void Draw(SpriteBatch sb)
    {
        var progress = Utils.GetLerpValue(0, MaxTime, Time, true);
        
        var texture = TextureAssets.Npc[ToCopy.type].Value;
        var origin = StartFrame.Size() / 2f;
        
        var effects = SpriteEffects.None;
        if (StartDirection < 1) effects = SpriteEffects.FlipHorizontally;
        
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
    }
}