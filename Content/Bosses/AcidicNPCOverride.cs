using System;
using System.IO;
using AcidicBosses.Common.Effects;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace AcidicBosses.Content.Bosses;

public abstract class AcidicNPCOverride : GlobalNPC
{
    public override bool InstancePerEntity => true;
    protected abstract int OverriddenNpc { get; }
    protected NPC Npc { get; private set; }
    
    /// <summary>
    /// 16 extra floats for synced ai stuff.
    /// That many may be overkill, but it's only an extra 64 bytes to sync so why not
    /// </summary>
    protected float[] ExtraAI = new float[16];

    private bool isFirstFrame = true;

    public override bool AppliesToEntity(NPC entity, bool lateInstantiation)
    {
        return entity.type == OverriddenNpc;
    }

    public virtual void OnFirstFrame(NPC npc)
    {
        
    }

    public sealed override bool PreAI(NPC npc)
    {
        if (isFirstFrame)
        {
            Npc = npc;
            OnFirstFrame(npc);
            isFirstFrame = false;
        }
        
        return AcidAI(npc);
    }

    public virtual bool AcidAI(NPC npc)
    {
        return true;
    }

    public virtual void SendAcidAI(NPC npc, BitWriter bitWriter, BinaryWriter binaryWriter)
    {
        
    }

    public sealed override void SendExtraAI(NPC npc, BitWriter bitWriter, BinaryWriter binaryWriter)
    {
        for (var i = 0; i < 16; i++)
        {
            binaryWriter.Write(ExtraAI[i]);
        }
        SendAcidAI(npc, bitWriter, binaryWriter);
    }
    
    public virtual void ReceiveAcidAI(NPC npc, BitReader bitReader, BinaryReader binaryReader)
    {
        
    }

    public sealed override void ReceiveExtraAI(NPC npc, BitReader bitReader, BinaryReader binaryReader)
    {
        for (var i = 0; i < 16; i++)
        {
            ExtraAI[i] = binaryReader.ReadSingle();
        }
        ReceiveAcidAI(npc, bitReader, binaryReader);
    }

    protected void ResetExtraAI()
    {
        for (var i = 0; i < 16; i++)
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
        if (npc.life <= 0)
        {
            EffectsManager.BossRageKill();
            EffectsManager.ShockwaveKill();
        }
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