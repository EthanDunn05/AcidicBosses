using System.IO;
using AcidicBosses.Common.Effects;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace AcidicBosses.Content.Bosses;

/// <summary>
/// This is pretty bare bones compared to the npc override. This is mostly here
/// to match the methods used in the npc override for better readability between the two.
/// </summary>
public abstract class AcidicNPC : ModNPC
{
    private bool isFirstFrame = true;

    public virtual void OnFirstFrame()
    {
        
    }

    public sealed override void AI()
    {
        if (isFirstFrame)
        {
            OnFirstFrame();
            isFirstFrame = false;
        }
        AcidAI();
    }

    public virtual void AcidAI() { }

    public virtual void SendAcidAI(BinaryWriter binaryWriter)
    {
        
    }

    public override void SendExtraAI(BinaryWriter writer)
    {
        SendAcidAI(writer);
    }

    public virtual void ReceiveAcidAI(BinaryReader binaryReader)
    {
        
    }

    public sealed override void ReceiveExtraAI(BinaryReader binaryReader)
    {
        ReceiveAcidAI(binaryReader);
    }

    protected void NetSync(bool onlySendFromServer = true)
    {
        if (onlySendFromServer && Main.netMode != NetmodeID.Server)
            return;
        
        if (Main.netMode != NetmodeID.SinglePlayer)
            NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, NPC.whoAmI);
    }

    protected virtual void LookTowards(Vector2 target, float power)
    {
        NPC.rotation = NPC.rotation.AngleLerp(NPC.AngleTo(target), power);
    }

    protected static bool IsTargetGone(NPC npc)
    {
        var player = Main.player[npc.target];
        return !player.active || player.dead;
    }
}