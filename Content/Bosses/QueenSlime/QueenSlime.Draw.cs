using AcidicBosses.Common;
using AcidicBosses.Common.Effects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;

namespace AcidicBosses.Content.Bosses.QueenSlime;

public partial class QueenSlime
{
    private bool drawWings = true;
    private bool drawExtraWings = false;
    private bool drawAfterimages = false;
    private bool drawCrown = true;
    private bool drawCore = true;
    
    private int wingFrameCounter = 0;
    private int wingFrame = 0;
    private const int wingFrameCount = 4;

    private bool flapping = false;
    private bool singleFlap = false;
    private bool waitingToFlap = true;
    
    private float squash = 0f;
    private Vector2 Scale => new Vector2(Npc.scale + squash * Npc.scale, Npc.scale - squash * Npc.scale);
    
    public override bool AcidicDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color lightColor)
    {
        var drawColor = lightColor;
        
        // Lots of spritebatch shenanigans
        if (drawAfterimages)
        {
            spriteBatch.EnterShader();
            DrawAfterimages(npc, spriteBatch, drawColor);
        }
        
        if (drawWings || drawExtraWings || drawCore) spriteBatch.ExitShader();
        if (drawExtraWings)
        {
            // Gray is darker than dark gray because reasons
            // Gray is (128, 128, 128)
            // Dark Gray is (169, 169, 169)
            DrawWings(npc, spriteBatch, drawColor.MultiplyRGBA(Color.Gray), new Vector2(25f, 40f), 2);
            DrawWings(npc, spriteBatch, drawColor.MultiplyRGBA(Color.DarkGray), new Vector2(12.5f, 20f), 1);
        }
        if (drawWings) DrawWings(npc, spriteBatch, drawColor, Vector2.Zero, 0);
        if (drawCore) DrawCore(npc, spriteBatch, drawColor);
        
        spriteBatch.EnterShader();
        DrawBody(npc, spriteBatch, drawColor);

        spriteBatch.ExitShader();
        if (drawCrown) DrawCrown(npc, spriteBatch, drawColor);

        return false;
    }

    private void DrawAfterimages(NPC npc, SpriteBatch spriteBatch, Color drawColor)
    {
        var bodyTex = TextureAssets.Npc[npc.type].Value;
        var drawPos = npc.Bottom - Main.screenPosition;
        drawPos.Y += 2f;
        
        var frameCount = Main.npcFrameCount[npc.type];
        var frame = bodyTex.Frame(2, 16, Frame / frameCount, Frame % frameCount);
        frame.Inflate(0, -2);
        
        var origin = frame.Size() * new Vector2(0.5f, 1f);
        
        GameShaders.Misc["QueenSlime"].Apply();
    
        for (var i = 1; i < npc.oldPos.Length; i++)
        {
            var fadeProgress = 0.5f * (10 - i) / 20f;
            var oldPos = npc.oldPos[i] + new Vector2((float)npc.width * 0.5f, npc.height);
            oldPos -= npc.Bottom - Vector2.Lerp(oldPos, npc.Bottom, 0.75f);
            oldPos -= Main.screenPosition;
            var fadedColor = drawColor * fadeProgress;
            
            spriteBatch.Draw(bodyTex, oldPos, frame, fadedColor, npc.rotation, origin,
                Scale, SpriteEffects.FlipHorizontally, 0f);
        }
    }

    private void DrawBody(NPC npc, SpriteBatch spriteBatch, Color drawColor)
    {
        var bodyTex = TextureAssets.Npc[npc.type].Value;
        var drawPos = npc.Bottom - Main.screenPosition;
        drawPos.Y += 2f;
        
        var frameCount = Main.npcFrameCount[npc.type];
        var frame = bodyTex.Frame(2, 16, Frame / frameCount, Frame % frameCount);
        frame.Inflate(0, -2);
        
        var origin = frame.Size() * new Vector2(0.5f, 1f);
        
        var drawData = new DrawData(bodyTex, drawPos, frame, npc.GetAlpha(drawColor),
            npc.rotation, origin, Scale, SpriteEffects.FlipHorizontally);
        GameShaders.Misc["QueenSlime"].Apply(drawData);
        drawData.Draw(spriteBatch);
        
        Main.pixelShader.CurrentTechnique.Passes[0].Apply();
    }

    private void DrawCore(NPC npc, SpriteBatch spriteBatch, Color drawColor)
    {
        var coreTex = TextureAssets.Extra[ExtrasID.QueenSlimeCrystalCore].Value;
        var rectangle4 = coreTex.Frame();
        var origin = rectangle4.Size() * new Vector2(0.5f, 0.5f);

        var center = npc.Center;
        center = CoreOffset(center);
        if (drawExtraWings) center += Main.rand.NextVector2Unit() * 2f;
        center -= Main.screenPosition;
        
        Main.pixelShader.CurrentTechnique.Passes[0].Apply();
        spriteBatch.Draw(coreTex, center, rectangle4, drawColor, npc.rotation, origin, 1f, SpriteEffects.FlipHorizontally, 0f);
        GameShaders.Misc["QueenSlime"].Apply();
    }

    private void DrawCrown(NPC npc, SpriteBatch spriteBatch, Color drawColor)
    {
        var crownTex = TextureAssets.Extra[ExtrasID.QueenSlimeCrown].Value;
        var frame = crownTex.Frame();
        var origin = frame.Size() * new Vector2(0.5f, 0.5f);
        var pos = GetCrownPos();
        pos -= Main.screenPosition;
        
        spriteBatch.Draw(crownTex, pos, frame, drawColor, npc.rotation, origin, 1f, SpriteEffects.FlipHorizontally, 0f);
    }

    private void DrawWings(NPC npc, SpriteBatch spriteBatch, Color drawColor, Vector2 offset, int wingIndex)
    {
        var wingTex = TextureAssets.Extra[ExtrasID.QueenSlimeWing].Value;
        var frame = wingTex.Frame(1, wingFrameCount, 0, wingFrame);
        var scale = 1f;

        for (var i = 0; i < 2; i++)
        {
            var originX = 1f;
            var xOffset = -offset.X + (Npc.width * (1f - Scale.X));
            var effects = SpriteEffects.None;

            if (i == 1)
            {
                originX = 0f;
                xOffset = 0f - xOffset + 2f;
                effects = SpriteEffects.FlipHorizontally;
            }

            var origin = frame.Size() * new Vector2(originX, 0.5f);
            var pos = new Vector2(npc.Center.X + xOffset, npc.Center.Y);
            pos = CoreOffset(pos);
            pos.Y += offset.Y * Scale.Y;
            if (npc.rotation != 0f)
                pos = pos.RotatedBy(npc.rotation, npc.Bottom);

            pos -= Main.screenPosition;
            var rotOff = MathHelper.Pi / 16f * wingIndex;
            var rotOffset = MathHelper.Clamp(npc.velocity.Y - wingIndex / 12f, -6f, 4f) * -0.1f + rotOff;
            if (i == 0) rotOffset *= -1f;

            spriteBatch.Draw(wingTex, pos, frame, drawColor, npc.rotation + rotOffset, origin, scale, effects, 0f);
        }
    }

    private Vector2 CoreOffset(Vector2 pos)
    {
        var centerYOff = 0f;
        switch (Frame)
        {
            case 1:
            case 6:
                centerYOff -= 10f;
                break;
            case 3:
            case 5:
                centerYOff += 10f;
                break;
            case 4:
            case 12:
            case 13:
            case 14:
            case 15:
                centerYOff += 18f;
                break;
            case 7:
            case 8:
                centerYOff -= 14f;
                break;
            case 9:
                centerYOff -= 16f;
                break;
            case 10:
                centerYOff -= 18f;
                break;
            case 11:
                centerYOff += 20f;
                break;
            case 20:
                centerYOff -= 14f;
                break;
            case 21:
            case 23:
                centerYOff -= 18f;
                break;
            case 22:
                centerYOff -= 22f;
                break;
        }

        pos.Y += (Npc.height * (1f - Scale.Y)) / 2f;
        pos.Y += centerYOff;
        if (Npc.rotation != 0f)
            pos = pos.RotatedBy(Npc.rotation, Npc.Bottom);

        return pos;
    }

    private Vector2 GetCrownPos()
    {
        var crownTex = TextureAssets.Extra[ExtrasID.QueenSlimeCrown].Value;
        var frame = crownTex.Frame();
        var pos = new Vector2(Npc.Center.X, Npc.Top.Y - (float)frame.Bottom + 44f);
        pos = CrownOffset(pos);
        return pos;
    }
    
    private Vector2 CrownOffset(Vector2 pos)
    {
        var centerYOff = 0f;
        switch (Frame)
        {
            case 1:
                centerYOff -= 10f;
                break;
            case 3:
            case 5:
            case 6:
                centerYOff += 10f;
                break;
            case 4:
            case 12:
            case 13:
            case 14:
            case 15:
                centerYOff += 18f;
                break;
            case 7:
            case 8:
                centerYOff -= 14f;
                break;
            case 9:
                centerYOff -= 16f;
                break;
            case 10:
                centerYOff -= 18f;
                break;
            case 11:
                centerYOff += 20f;
                break;
            case 20:
                centerYOff -= 14f;
                break;
            case 21:
            case 23:
                centerYOff -= 18f;
                break;
            case 22:
                centerYOff -= 22f;
                break;
        }

        pos.Y += Npc.height * (1f - Scale.Y);
        pos.Y += centerYOff;
        if (Npc.rotation != 0f)
            pos = pos.RotatedBy(Npc.rotation, Npc.Bottom);

        return pos;
    }

    public override void FindFrame(NPC npc, int frameHeight)
    {
        // Wing Flapping
        if (singleFlap || flapping)
        {
            if (wingFrame == 1 && wingFrameCounter == 3)
            {
                SoundEngine.PlaySound(SoundID.Item1 with { Pitch = -1f}, Npc.Center);
            }
            
            wingFrameCounter++;
            if (wingFrameCounter >= 6)
            {
                wingFrame++;
                if (wingFrame >= wingFrameCount)
                {
                    wingFrame = 0;
                    singleFlap = false;
                }

                wingFrameCounter = 0;
            }
        }
        else
        {
            wingFrame = 0;
            wingFrameCounter = 4;
        }
        
        // Body Animation
        FrameCounter++;
        if (npc.velocity.Y < 0f || flapping)
        {
            if (Frame < 20 || Frame > 23) {
                if (Frame < 4 || Frame > 7) {
                    Frame = 4;
                    FrameCounter = 0;
                }

                if (FrameCounter >= 4.0) {
                    FrameCounter = 0;
                    Frame++;
                    if (Frame >= 7)
                    {
                        Frame = 7;
                        if (flapping) Frame = 22;
                    }
                }
            }
            else if (FrameCounter >= 5.0) {
                FrameCounter = 0;
                Frame++;
                if (Frame >= 24) Frame = 20;
            }
        }
        else if (npc.velocity.Y > 0f)
        {
            if (Frame < 8 || Frame > 10) {
                Frame = 8;
                FrameCounter = 0;
            }

            if (FrameCounter >= 8) {
                FrameCounter = 0;
                Frame++;
                if (Frame >= 10) Frame = 10;
            }
        }
        else
        {
            if (FrameCounter >= 10)
            {
                Frame++;
                FrameCounter = 0;
            }

            if (Frame >= 4)
            {
                Frame = 0;
            }
        }
    }
}