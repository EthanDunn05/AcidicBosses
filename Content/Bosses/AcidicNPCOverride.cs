using System;
using System.IO;
using System.Linq;
using AcidicBosses.Common.Effects;
using AcidicBosses.Core.Systems;
using AcidicBosses.Core.Systems.DifficultySystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace AcidicBosses.Content.Bosses;

/// <summary>
/// Inherit this when overriding the behavior of a vanilla npc.
/// </summary>
public abstract class AcidicNPCOverride : GlobalNPC
{
    // Always
    public override bool InstancePerEntity => true;
    
    /// <summary>
    /// The NPCID of the npc to override
    /// </summary>
    protected abstract int OverriddenNpc { get; }
    protected NPC Npc { get; private set; }
    
    /// <summary>
    /// 4 extra floats for synced ai stuff.
    /// </summary>
    protected float[] ExtraAI = new float[4];

    private bool isFirstFrame = true;

    private static bool AcidicActive => AcidicDifficultySystem.AcidicActive;

    public override bool AppliesToEntity(NPC entity, bool lateInstantiation)
    {
        return entity.type == OverriddenNpc;
    }

    /// <summary>
    /// Called the first frame the NPC is alive on both the server and client
    /// </summary>
    /// <param name="npc">Not really useful anymore, but keeping it because I'm lazy</param>
    public virtual void OnFirstFrame(NPC npc)
    {
        
    }
    
    public sealed override bool PreAI(NPC npc)
    {
        if (!AcidicActive) return true;
        
        // First frame setup
        if (isFirstFrame)
        {
            Npc = npc;
            OnFirstFrame(npc);
            isFirstFrame = false;
        }
        
        return AcidAI(npc);
    }

    /// <summary>
    /// The main AI method of an Acidic NPC.
    /// This is where the primary logic takes place.
    /// </summary>
    /// <param name="npc">The npc</param>
    /// <returns>True to run vanilla AI, false to not. Same as PreAI</returns>
    public virtual bool AcidAI(NPC npc)
    {
        return true;
    }

    /// <summary>
    /// Just set the target to a random valid player
    /// </summary>
    protected void TargetRandom()
    {
        Npc.target = RandomTargetablePlayer().whoAmI;
    }

    // Gets a random player close enough to the boss to be targeted
    protected Player RandomTargetablePlayer()
    {
        const float maxDistance = 2000;
        var players = Main.player
            .Where(p => p.active && !p.dead && Npc.Distance(p.Center) < maxDistance)
            .ToList();
        var player = Main.rand.NextFromCollection(players);

        return player;
    }

    /// <summary>
    /// Send stuff to be synced between server and client.
    /// Sending and receiving must be done in the same order.
    /// </summary>
    /// <param name="npc">The npc</param>
    /// <param name="bitWriter">Write booleans</param>
    /// <param name="binaryWriter">Write everything else</param>
    public virtual void SendAcidAI(NPC npc, BitWriter bitWriter, BinaryWriter binaryWriter)
    {
        
    }

    public sealed override void SendExtraAI(NPC npc, BitWriter bitWriter, BinaryWriter binaryWriter)
    {
        if (!AcidicActive) return;
        
        for (var i = 0; i < 4; i++)
        {
            binaryWriter.Write(ExtraAI[i]);
        }
        SendAcidAI(npc, bitWriter, binaryWriter);
    }
    
    /// <summary>
    /// Read stuff to be synced between server and client.
    /// Sending and receiving must be done in the same order.
    /// </summary>
    /// <param name="npc">The npc</param>
    /// <param name="bitReader">Read booleans</param>
    /// <param name="binaryReader">Read everything else</param>
    public virtual void ReceiveAcidAI(NPC npc, BitReader bitReader, BinaryReader binaryReader)
    {
        
    }

    public sealed override void ReceiveExtraAI(NPC npc, BitReader bitReader, BinaryReader binaryReader)
    {
        if (!AcidicActive) return;
        
        for (var i = 0; i < 4; i++)
        {
            ExtraAI[i] = binaryReader.ReadSingle();
        }
        ReceiveAcidAI(npc, bitReader, binaryReader);
    }

    protected void ResetExtraAI()
    {
        for (var i = 0; i < 4; i++)
        {
            ExtraAI[i] = 0f;
        }
    }

    protected static void NetSync(NPC npc, bool onlySendFromServer = true)
    {
        if (onlySendFromServer && Main.netMode != NetmodeID.Server)
            return;
        
        if (Main.netMode != NetmodeID.SinglePlayer)
            NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, npc.whoAmI);
    }
    
    public override void HitEffect(NPC npc, NPC.HitInfo hit)
    {
        if (!AcidicActive) return;
        
        if (npc.life <= 0)
        {
            EffectsManager.BossRageKill();
            EffectsManager.ShockwaveKill();
        }
    }

    // Wrapper for PreDraw
    public virtual bool AcidicDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color lightColor)
    {
        return true;
    }

    public sealed override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
    {
        if (!AcidicActive) return true;
        return AcidicDraw(npc, spriteBatch, screenPos, drawColor);
    }

    protected virtual void LookTowards(Vector2 target, float power)
    {
        Npc.rotation = Npc.rotation.AngleLerp(Npc.AngleTo(target), power);
    }

    protected static bool IsTargetGone(NPC npc)
    {
        var player = Main.player[npc.target];
        return !player.active || player.dead;
    }
}