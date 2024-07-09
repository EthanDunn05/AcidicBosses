using System;
using AcidicBosses.Common.Configs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace AcidicBosses.Content.Bosses.Skeletron;

public class SkeletronHand : AcidicNPCOverride
{
    protected override int OverriddenNpc => NPCID.SkeletronHand;
    
    protected override bool BossEnabled => BossToggleConfig.Get().EnableSkeletron;

    // -1 for left, 1 for right
    private int ArmSide => (int) -Npc.ai[0];

    private int HeadID => (int) Npc.ai[1];

    private NPC Head => Main.npc[HeadID];
    
    private SkeletronHead.HandState HeadAttack
    {
        get => (SkeletronHead.HandState) Head.ai[3];
        set => Head.ai[3] = (float) value;
    }

    #region AI

    private bool countUpTimer = false;

    private bool isFleeing = false;

    private int AiTimer
    {
        get => (int) Npc.ai[2];
        set => Npc.ai[2] = value;
    }

    public override void OnFirstFrame(NPC npc)
    {
        Npc.spriteDirection = -ArmSide;
        Npc.damage = (int) (Npc.damage * 1.5); // Way too weak in vanilla
    }

    public override bool AcidAI(NPC npc)
    {
        Npc.realLife = HeadID;
        if (Head.life > 0) Npc.life = Head.life;

        Npc.target = Head.target;

        if (AiTimer > 0 && !countUpTimer)
            AiTimer--;

        if (!Head.active || Head.aiStyle != NPCAIStyleID.SkeletronHead)
        {
            AiTimer += 10;
            if (AiTimer > 50 || Main.netMode != NetmodeID.Server)
            {
                Npc.life = -1;
                Npc.HitEffect();
                Npc.active = false;
            }

            return false;
        }

        // Don't interact when above skeletron
        if (HeadAttack == SkeletronHead.HandState.NoInteractLockHead)
        {
            Npc.damage = 0;
            Npc.dontTakeDamage = true;
            Npc.alpha = 150;
        }
        else
        {
            Npc.damage = Npc.defDamage;
            Npc.dontTakeDamage = false;
            Npc.alpha = 0;
        }

        switch (HeadAttack)
        {
            case SkeletronHead.HandState.HoverSide:
                Attack_HoverToSide();
                break;
            case SkeletronHead.HandState.LockSide:
                Attack_LockToSide();
                break;
            case SkeletronHead.HandState.LockHead:
                Attack_LockAboveHead();
                break;
            case SkeletronHead.HandState.NoInteractLockHead:
                Attack_LockAboveHead();
                break;
            case SkeletronHead.HandState.Slap:
                Attack_Slap();
                break;
            case SkeletronHead.HandState.AlternatingSlaps:
                Attack_AlternatingSlaps();
                break;
        }
        

        if (countUpTimer)
            AiTimer++;

        return false;
    }

    #region Attacks

    private void Attack_HoverToSide()
    {
        countUpTimer = false;
        var offsetTarget = new Vector2(ArmSide * 300f, -120);
        var target = Main.player[Npc.target].Center + offsetTarget;

        Npc.rotation = MathHelper.Lerp(Npc.rotation, MathHelper.Pi, 0.05f);

        Npc.SimpleFlyMovement(Npc.DirectionTo(target) * 15f, 0.5f);
    }
    
    private void Attack_LockToSide()
    {
        countUpTimer = false;
        var offsetTarget = new Vector2(ArmSide * 300f, -120);
        var target = Main.player[Npc.target].Center + offsetTarget;

        Npc.velocity = Vector2.Lerp(Npc.velocity, Vector2.Zero, 0.075f);
        Npc.rotation = MathHelper.Lerp(Npc.rotation, MathHelper.Pi, 0.05f);

        Npc.Center = Vector2.Lerp(Npc.Center, target, 0.075f);
    }

    private void Attack_LockAboveHead()
    {
        countUpTimer = false;
        var offsetTarget = new Vector2(ArmSide * 200f, -120);
        var target = Head.Center + offsetTarget;

        Npc.velocity = Vector2.Lerp(Npc.velocity, Vector2.Zero, 0.075f);
        Npc.rotation = MathHelper.Lerp(Npc.rotation, MathHelper.Pi, 0.05f);

        Npc.Center = Vector2.Lerp(Npc.Center, target, 0.075f);
    }
    
    private const int DashTrackTime = 20;
    private const int DashAtTime = 30;
    private const int DashLength = 30;

    public static int SlapLength = DashAtTime + DashLength;
    private void Attack_Slap()
    {
        countUpTimer = true;
        var target = Main.player[Head.target].Center;

        if (AiTimer == 0 && Main.netMode != NetmodeID.MultiplayerClient)
        {
            // var line = NewDashLine(Npc.Center, MathHelper.PiOver2);
            // line.timeLeft = DashAtTime;
        }
        if (AiTimer < DashTrackTime)
        {
            PointTowards(target, 0.25f);
            Npc.SimpleFlyMovement((Npc.rotation + MathHelper.PiOver2).ToRotationVector2() * -2.5f, 0.75f);
        }
        else if (AiTimer == DashAtTime)
        {
            Npc.velocity = (Npc.rotation + MathHelper.PiOver2).ToRotationVector2() * 17.5f;
            SoundEngine.PlaySound(SoundID.Item1, Npc.Center);
        }
        else if (AiTimer >= DashAtTime + DashLength)
        {
            countUpTimer = false;
            AiTimer = 0;
        }
    }

    private void Attack_AlternatingSlaps()
    {
        countUpTimer = true;
        var target = Main.player[Head.target].Center;
        
        var offset = 0;
        if (ArmSide == 1) offset = (DashAtTime + DashLength) / 2;

        if (AiTimer == 0 && Main.netMode != NetmodeID.MultiplayerClient)
        {
            
        }
        if (AiTimer < DashTrackTime + offset)
        {
            PointTowards(target, 0.25f);
            var offsetTarget = new Vector2(ArmSide * 500f, -120);
            var goal = target + offsetTarget;
            Npc.SimpleFlyMovement(Npc.DirectionTo(goal) * 10f, 0.5f);
        }
        else if (AiTimer == DashAtTime + offset)
        {
            Npc.velocity = (Npc.rotation + MathHelper.PiOver2).ToRotationVector2() * 17.5f;
            SoundEngine.PlaySound(SoundID.Item1, Npc.Center);
        }
        else if (AiTimer >= DashAtTime + DashLength + offset)
        {
            countUpTimer = false;
            AiTimer = offset;
        }
    }
    
    private Projectile NewDashLine(Vector2 position, float offset, bool anchorToBoss = true)
    {
        var ai1 = anchorToBoss ? Npc.whoAmI + 1 : 0;
        return Projectile.NewProjectileDirect(Npc.GetSource_FromAI(), position, Vector2.Zero,
            ModContent.ProjectileType<SkeletronHandDashLine>(), 0, 0, ai0: offset, ai1: ai1);
    }

    #endregion

    #endregion
    
    protected void PointTowards(Vector2 target, float power)
    {
        Npc.rotation = Npc.rotation.AngleLerp(Npc.AngleTo(target) - MathHelper.PiOver2, power);
    }
}