using System;
using System.IO;
using AcidicBosses.Common.Configs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader.IO;

namespace AcidicBosses.Content.Bosses.Twins;

public class Spazmatism : AcidicNPCOverride
{
    protected override int OverriddenNpc => NPCID.Spazmatism;
    protected override bool BossEnabled => BossToggleConfig.Get().EnableTwins;
    
    public bool UseAfterimages = false;
    public bool MechForm = false;

    public float Mass = 1f;

    #region AI
    
    private int Controller
    {
        get => (int) Npc.ai[0];
        set => Npc.ai[0] = value;
    }
    
    public override void OnFirstFrame(NPC npc)
    {
        if (Main.netMode != NetmodeID.MultiplayerClient)
        {
            Controller = TwinsController.Link(npc);
            Main.npc[Controller].ai[0] = Npc.whoAmI;
        }
    }

    public override bool AcidAI(NPC npc)
    {
        return false;
    }
    
    #endregion
    
    #region Drawing

    public override bool AcidicDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
    {
        var effects = SpriteEffects.None;
        if (npc.spriteDirection == 1)
            effects = SpriteEffects.FlipHorizontally;

        // Draw it :)
        var drawPos = npc.Center - Main.screenPosition;
        var eyeTexture = TextureAssets.Npc[npc.type].Value;
        var eyeOrigin = eyeTexture.Size() / new Vector2(1f, Main.npcFrameCount[npc.type]) * 0.5f;

        // Afterimages
        if (UseAfterimages)
            for (var i = 1; i < npc.oldPos.Length; i++)
            {
                // All of this is heavily simplified from decompiled vanilla
                var fade = 0.5f * (10 - i) / 20f;
                var afterImageColor = Color.Multiply(drawColor, fade);

                var pos = npc.oldPos[i] + new Vector2(npc.width, npc.height) / 2f - Main.screenPosition;
                spriteBatch.Draw(eyeTexture, pos, npc.frame, afterImageColor, npc.rotation, eyeOrigin, npc.scale,
                    effects,
                    0f);
            }

        spriteBatch.Draw(
            eyeTexture, drawPos,
            npc.frame, npc.GetAlpha(drawColor),
            npc.rotation, eyeOrigin, npc.scale,
            effects, 0f);

        return false;
    }
    
    public override void FindFrame(NPC npc, int frameHeight)
    {
        if (!ShouldOverride()) base.FindFrame(npc, frameHeight);
        
        if (npc.frameCounter < 7.0)
        {
            npc.frame.Y = 0;
        }
        else if (npc.frameCounter < 14.0)
        {
            npc.frame.Y = frameHeight;
        }
        else if (npc.frameCounter < 21.0)
        {
            npc.frame.Y = frameHeight * 2;
        }
        else
        {
            npc.frameCounter = 0.0;
            npc.frame.Y = 0;
        }

        // Show Mouth by offsetting the frame
        if (MechForm)
        {
            npc.frame.Y += frameHeight * 3;
        }
    }

    public override void BossHeadSlot(NPC npc, ref int index)
    {
        if (!ShouldOverride()) return;
        index = MechForm ? 1 : 0;
    }
    
    #endregion

    public override void SendAcidAI(BitWriter bitWriter, BinaryWriter binaryWriter)
    {
        bitWriter.WriteBit(UseAfterimages);
        bitWriter.WriteBit(MechForm);
        binaryWriter.Write(Mass);
    }

    public override void ReceiveAcidAI(BitReader bitReader, BinaryReader binaryReader)
    {
        UseAfterimages = bitReader.ReadBit();
        MechForm = bitReader.ReadBit();
        Mass = binaryReader.ReadSingle();
    }
    
    public override void LookTowards(Vector2 target, float power)
    {
        Npc.rotation = Npc.rotation.AngleLerp(Npc.AngleTo(target) - MathHelper.PiOver2, power);
    }
}