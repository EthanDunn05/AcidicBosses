using System;
using System.IO;
using System.Linq;
using AcidicBosses.Common.Effects;
using AcidicBosses.Core.StateManagement;
using AcidicBosses.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.ItemDropRules;
using Terraria.Graphics.CameraModifiers;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace AcidicBosses.Content.Bosses.BoC;

public class BoC : AcidicNPCOverride
{
    protected override int OverriddenNpc => NPCID.BrainofCthulhu;

    private BoCBossBar BossBar => (BoCBossBar) Npc.BossBar;

    public override void SetDefaults(NPC entity)
    {
        base.SetDefaults(entity);

        entity.BossBar = ModContent.GetInstance<BoCBossBar>();
        entity.knockBackResist = 0f; // Remove knockback
        entity.lifeMax = (int) (entity.lifeMax * 1.5f); // Compensate for fewer Creepers
    }

    #region AI

    private PhaseTracker phaseTracker;

    private bool isBrainOpen = false;

    private bool showPhantoms = false;

    private bool isFleeing = false;
    
    public override void OnFirstFrame(NPC npc)
    {
        NPC.crimsonBoss = npc.whoAmI;

        phaseTracker = new PhaseTracker([
            PhaseIntro,
            PhaseCreeperOne,
            PhaseAngerOne,
            PhaseTransitionOne,
            PhaseCreeperTwo,
            PhaseAngerTwo,
            PhaseTransitionTwo,
            PhaseAngerThree
        ]);
        
        CloseBrain();
    }

    public override bool AcidAI(NPC npc)
    {
        // Flee when no players are alive or out of crimson
        var target = Main.player[npc.target];
        if ((IsTargetGone(npc) || !target.ZoneCrimson) && !isFleeing)
        {
            npc.TargetClosest();
            target = Main.player[npc.target];
            if (IsTargetGone(npc) || !target.ZoneCrimson)
            {
                AttackManager.CountUp = true;
                isFleeing = true;
                AttackManager.AiTimer = 0;
            }
        }

        if (isFleeing) FleeAI();
        else phaseTracker.RunPhaseAI();

        return false;
    }
    
    private void FleeAI()
    {
        AttackManager.CountUp = true;

        var target = Main.player[Npc.target];
        if (!IsTargetGone(Npc) && target.ZoneCrimson)
        {
            AttackManager.CountUp = false;
            AttackManager.AiTimer = 0;
            isFleeing = false;
            return;
        }

        if (AttackManager.AiTimer < 120)
        {
            Npc.velocity.Y += AttackManager.AiTimer * 0.025f;
        }
        else
        {
            Npc.active = false;
            EffectsManager.BossRageKill();
            EffectsManager.ShockwaveKill();
        }
    }
    
    #endregion

    #region Phase AIs

    private PhaseState PhaseIntro => new(Phase_Intro);
    
    private void Phase_Intro()
    {
        AttackManager.CountUp = true;
        BossBar.MaxCreepers = 10;

        // 10 creepers
        if (AttackManager.AiTimer % 6 == 0 && AttackManager.AiTimer < 60) Attack_SummonCreeper(CreeperOverride.AttackType.Dash);

        if (AttackManager.AiTimer >= 60)
        {
            SoundEngine.PlaySound(SoundID.Roar);

            phaseTracker.NextPhase();
            AttackManager.Reset();
        }
    }

    private PhaseState PhaseCreeperOne => new(Phase_CreeperOne);

    private void Phase_CreeperOne()
    {
        var creepersAlive = Main.npc.Count(n => n.type == NPCID.Creeper && n.active);

        if (creepersAlive <= 0)
        {
            AttackManager.Reset();
            OpenBrain();
            phaseTracker.NextPhase();
            return;
        }

        // Slow boi
        Attack_HoverToPlayer(0.5f);
    }

    private PhaseState PhaseAngerOne => new(Phase_AngerOne, EnterPhaseAngerOne);

    private void EnterPhaseAngerOne()
    {
        var teleport = new AttackState(() => Attack_Teleport(1.5f), 120);
        var tripleIchor = new AttackState(Attack_TripleIchorShot, 120);
        
        AttackManager.SetAttackPattern([
            teleport,
            tripleIchor
        ]);
    }

    private void Phase_AngerOne()
    {
        if (Npc.GetLifePercent() <= 0.6f && !AttackManager.CountUp)
        {
            ResetExtraAI();
            phaseTracker.NextPhase();
            AttackManager.Reset();
            return;
        }

        if (AttackManager.AiTimer > 0 && !AttackManager.CountUp)
        {
            Attack_HoverToPlayer(1.25f);
            return;
        }

        AttackManager.RunAttackPattern();
    }

    private PhaseState PhaseTransitionOne => new(Phase_TransitionOne);

    private void Phase_TransitionOne()
    {
        BossBar.MaxCreepers = 10;
        AttackManager.CountUp = true;

        if (AttackManager.AiTimer == 0)
        {
            CloseBrain();
            SoundEngine.PlaySound(SoundID.Roar, Npc.Center);
        }

        if (AttackManager.AiTimer % 6 == 0 && AttackManager.AiTimer < 60) Attack_SummonCreeper(CreeperOverride.AttackType.SuperDash);
        if (AttackManager.AiTimer >= 60)
        {
            ExtraAI[0] = 1;
            phaseTracker.NextPhase();
            AttackManager.Reset();
        }
    }

    private PhaseState PhaseCreeperTwo => new(Phase_CreeperTwo);

    private void Phase_CreeperTwo()
    {
        var creepersAlive = Main.npc.Count(n => n.type == NPCID.Creeper && n.active);

        if (creepersAlive <= 0)
        {
            Npc.dontTakeDamage = false;
            AttackManager.Reset();
            OpenBrain();
            phaseTracker.NextPhase();
            return;
        }

        if (AttackManager.AiTimer > 0 && !AttackManager.CountUp)
        {
            Attack_HoverToPlayer(1f);
        }
        else
        {
            if ( Attack_Teleport(1f)) AttackManager.AiTimer = 160;
        }
    }

    private PhaseState PhaseAngerTwo => new(Phase_AngerTwo, EnterPhaseAngerTwo);
    
    private void EnterPhaseAngerTwo()
    {
        var teleport = new AttackState(() => Attack_Teleport(2f), 120);
        var tripleIchor = new AttackState(Attack_TripleIchorShot, 90);
        var summon = new AttackState(() =>
        {
            BossBar.MaxCreepers = 0; // No Shield
            return Attack_SummonCreeper(CreeperOverride.AttackType.Dash);
        }, 90);
        
        AttackManager.SetAttackPattern([
            teleport,
            tripleIchor,
            tripleIchor,
            teleport,
            summon
        ]);
    }

    private void Phase_AngerTwo()
    {
        if (Npc.GetLifePercent() <= 0.25f && !AttackManager.CountUp)
        {
            ResetExtraAI();
            phaseTracker.NextPhase();
            AttackManager.Reset();
            return;
        }
        
        if (AttackManager.AiTimer > 0 && !AttackManager.CountUp)
        {
            Attack_HoverToPlayer(1.75f);
            return;
        }

        AttackManager.RunAttackPattern();
    }

    private PhaseState PhaseTransitionTwo => new(Phase_TransitionTwo);

    private void Phase_TransitionTwo()
    {
        BossBar.MaxCreepers = 0;
        AttackManager.CountUp = true;

        if (AttackManager.AiTimer == 0)
        {
            SoundEngine.PlaySound(SoundID.Roar, Npc.Center);
            EffectsManager.ShockwaveActive(Npc.Center, 0.075f, 0.15f, Color.Transparent);
            var punch = new PunchCameraModifier(Npc.Center, Main.rand.NextVector2Unit(), 10f, 12f, 60, 1000f, FullName);
            Main.instance.CameraModifiers.Add(punch);
            showPhantoms = true;
            
            Npc.velocity = Vector2.Zero;
        }
        // Shockwave
        else if (AttackManager.AiTimer < 120)
        {
            var shockT = AttackManager.AiTimer / 120f;
            EffectsManager.ShockwaveProgress(shockT);
            ConfusePlayers();
        }
        else
        {
            EffectsManager.ShockwaveKill();
            
            AttackManager.Reset();
            phaseTracker.NextPhase();
        }
    }

    private PhaseState PhaseAngerThree => new(Phase_AngerThree, EnterPhaseAngerThree);

    private void EnterPhaseAngerThree()
    {
        var teleport = new AttackState(() => Attack_Teleport(2f), 90);
        var tripleIchor = new AttackState(Attack_TripleIchorShot, 60);
        var summon = new AttackState(() =>
        {
            BossBar.MaxCreepers = 0; // No Shield
            return Attack_SummonCreeper(CreeperOverride.AttackType.Dash);
        }, 90);
        
        AttackManager.SetAttackPattern([
            teleport,
            tripleIchor,
            summon,
            teleport,
            tripleIchor,
            tripleIchor
        ]);
    }

    private void Phase_AngerThree()
    {
        ConfusePlayers();
        
        if (AttackManager.AiTimer > 0 && !AttackManager.CountUp)
        {
            Attack_HoverToPlayer(1f);
            return;
        }

        AttackManager.RunAttackPattern();
    }

    #endregion

    #region Attack Behaviors

    private bool Attack_HoverToPlayer(float speed)
    {
        var target = Main.player[Npc.target].Center;
        var direction = Npc.Center.DirectionTo(target);
        Npc.SimpleFlyMovement(direction * speed * MathF.Sqrt(5 * Npc.Distance(target)) / 10f, 0.05f);

        return true;
    }

    private bool Attack_SummonCreeper(CreeperOverride.AttackType type)
    {
        SoundEngine.PlaySound(SoundID.NPCHit9, Npc.Center);

        if (Main.netMode != NetmodeID.MultiplayerClient)
        {
            var pos = Npc.Center + Main.rand.NextVector2Circular(250, 250);
            NPC.NewNPCDirect(Npc.GetSource_FromAI(), pos, NPCID.Creeper, ai1: (int) type);
        }

        return true;
    }

    private bool Attack_TripleIchorShot()
    {
        const float spread = MathF.PI / 6f;
        const float speed = 5f;

        for (var i = -1; i <= 1; i++)
        {
            var angleOffset = spread * i;
            var target = Main.player[Npc.target].Center;
            var angle = Npc.DirectionTo(target).ToRotation() + angleOffset;

            NewIchorShot(Npc.Center, angle.ToRotationVector2() * speed);
        }

        return true;
    }

    private bool Attack_Teleport(float hoverSpeed)
    {
        const int fadeTime = 45;
        ref var offsetX = ref ExtraAI[0];
        ref var offsetY = ref ExtraAI[1];

        AttackManager.CountUp = true;
        var isDone = false;

        if (AttackManager.AiTimer == 0 && Main.netMode != NetmodeID.MultiplayerClient)
        {
            var target = Main.player[Npc.target].Center;

            var distance = MathF.Min(Npc.Distance(target), 750); // Don't teleport too far
            distance = MathF.Max(distance, 250); // Nor too close
            
            var pos = Main.rand.NextVector2Unit() * distance;
            offsetX = pos.X;
            offsetY = pos.Y;
            NetSync(Npc);
        }

        switch (AttackManager.AiTimer)
        {
            // Fade out
            case < fadeTime:
            {
                Attack_HoverToPlayer(hoverSpeed);

                var fadeT = EasingHelper.QuadOut((float) AttackManager.AiTimer / fadeTime);
                Npc.Opacity = 1f - fadeT;
                break;
            }
            // At Teleport
            case fadeTime:
            {
                var target = Main.player[Npc.target].Center;

                Npc.velocity = Vector2.Zero;
                Npc.position = target + new Vector2(offsetX, offsetY);
                break;
            }
            // Fade in
            case < fadeTime * 2:
            {
                Attack_HoverToPlayer(hoverSpeed);

                var fadeT = EasingHelper.QuadIn((float) (AttackManager.AiTimer - fadeTime) / fadeTime);
                Npc.Opacity = fadeT;
                break;
            }
            // Done
            case >= fadeTime * 2:
            {
                isDone = true;
                AttackManager.CountUp = false;
                Npc.Opacity = 1f;
                ResetExtraAI();
                break;
            }
        }

        return isDone;
    }

    private void ConfusePlayers()
    {
        // Confuse all players
        for (var i = 0; i < Main.player.Length; i++)
        {
            var player = Main.player[i];
            if (!player.active || player.dead) continue;

            if (!player.HasBuff(BuffID.Confused))
            {
                // 5 seconds
                player.AddBuff(BuffID.Confused, 2);
            }
            else
            {
                // Refresh if confusion has less than a second left
                var buffSlot = player.buffType.First(b => b == BuffID.Confused);
                if(player.buffTime[buffSlot] < 60) player.AddBuff(BuffID.Confused, 120);
            }
        }
    }

    private void OpenBrain()
    {
        Npc.dontTakeDamage = false;
        isBrainOpen = true;

        // Just taken from vanilla
        SoundEngine.PlaySound(SoundID.NPCHit9, Npc.Center);
        Gore.NewGore(Npc.GetSource_FromAI(), Npc.Center,
            new Vector2(Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f), 392);
        Gore.NewGore(Npc.GetSource_FromAI(), Npc.Center,
            new Vector2(Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f), 393);
        Gore.NewGore(Npc.GetSource_FromAI(), Npc.Center,
            new Vector2(Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f), 394);
        Gore.NewGore(Npc.GetSource_FromAI(), Npc.Center,
            new Vector2(Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f), 395);
        for (var num1414 = 0; num1414 < 20; num1414++)
        {
            Dust.NewDust(Npc.position, Npc.width, Npc.height, DustID.Blood, Main.rand.Next(-30, 31) * 0.2f,
                Main.rand.Next(-30, 31) * 0.2f);
        }

        SoundEngine.PlaySound(SoundID.Roar, Npc.Center);
    }

    private void CloseBrain()
    {
        // No fancy effects for now
        Npc.dontTakeDamage = true;
        isBrainOpen = false;
    }

    private Projectile NewIchorShot(Vector2 position, Vector2 velocity)
    {
        return Projectile.NewProjectileDirect(Npc.GetSource_FromAI(), position, velocity,
            ModContent.ProjectileType<IchorShot>(),
            Npc.damage / 4, 3);
    }

    #endregion

    #region Drawing

    public override bool AcidicDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color lightColor)
    {
        var drawPos = npc.Center - Main.screenPosition;
        var brainTexture = TextureAssets.Npc[npc.type].Value;
        var origin = npc.frame.Size() * 0.5f;

        // I have to do this workaround to offset the frame when the brain is open.
        // This game's code is so spaghetti that it won't go past 4 frames and I have no clue why.
        var frame = npc.frame;
        if (isBrainOpen) frame.Y += npc.frame.Height * 4;

        // For fading on teleporting
        lightColor *= npc.Opacity;
        
        // Phantoms
        if (showPhantoms)
        {
            for (var i = 0; i < 4; i++)
            {
                var phantomPos = new Vector2();
                var offsetX = Math.Abs(npc.Center.X - Main.player[Main.myPlayer].Center.X);
                var offsetY = Math.Abs(npc.Center.Y - Main.player[Main.myPlayer].Center.Y);
                
                if (i is 0 or 2) phantomPos.X = Main.player[Main.myPlayer].Center.X + offsetX;
                else phantomPos.X = Main.player[Main.myPlayer].Center.X - offsetX;
                
                if (i is 0 or 1) phantomPos.Y = Main.player[Main.myPlayer].Center.Y + offsetY;
                else phantomPos.Y = Main.player[Main.myPlayer].Center.Y - offsetY;
                
                var phantomColor = Lighting.GetColor(phantomPos.ToTileCoordinates()) * 0.5f;
                
                spriteBatch.Draw(
                    brainTexture, phantomPos - Main.screenPosition, 
                    frame, phantomColor, 
                    npc.rotation, origin, npc.scale, 
                    SpriteEffects.None, 0f);
            }
        }

        spriteBatch.Draw(
            brainTexture, drawPos,
            frame, lightColor,
            npc.rotation, origin, npc.scale,
            SpriteEffects.None, 0f);

        return false;
    }

    public override void FindFrame(NPC npc, int frameHeight)
    {
        if (npc.frameCounter > 6.0)
        {
            npc.frameCounter = 0.0;
            npc.frame.Y += frameHeight;
        }

        if (npc.frame.Y > frameHeight * 3) npc.frame.Y = 0;
    }

    #endregion
    
    public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
    {
        // Drop Tissue Samples directly if the player isn't getting a treasure bag
        var notExpertRule = new LeadingConditionRule(new Conditions.NotExpert());
        notExpertRule.OnSuccess(ItemDropRule.Common(ItemID.TissueSample, 1, 75, 125));

        npcLoot.Add(notExpertRule);
    }
    
    public override void SendAcidAI(BitWriter bitWriter, BinaryWriter binaryWriter)
    {
        phaseTracker.Serialize(binaryWriter);
        
        bitWriter.WriteBit(isFleeing);
    }

    public override void ReceiveAcidAI(BitReader bitReader, BinaryReader binaryReader)
    {
        phaseTracker.Deserialize(binaryReader);
        
        isFleeing = bitReader.ReadBit();
    }
}