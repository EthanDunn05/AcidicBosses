using System;
using System.Linq;
using AcidicBosses.Common.Effects;
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
        entity.lifeMax = (int) (entity.lifeMax * 1.5f); // Compensate for less Creepers
    }


    #region Phases

    private enum PhaseState
    {
        Intro,
        CreeperOne,
        AngerOne,
        TransitionOne,
        CreeperTwo,
        AngerTwo,
        TransitionTwo,
        AngerThree
    }

    private PhaseState CurrentPhase
    {
        get => (PhaseState) Npc.ai[1];
        set => Npc.ai[1] = (float) value;
    }
    
    private Action CurrentAi => CurrentPhase switch
    {
        PhaseState.Intro => Phase_Intro,
        PhaseState.CreeperOne => Phase_CreeperOne,
        PhaseState.AngerOne => Phase_AngerOne,
        PhaseState.TransitionOne => Phase_TransitionOne,
        PhaseState.CreeperTwo => Phase_CreeperTwo,
        PhaseState.AngerTwo => Phase_AngerTwo,
        PhaseState.TransitionTwo => Phase_TransitionTwo,
        PhaseState.AngerThree => Phase_AngerThree,
        _ => throw new UsageException(
            $"BoC is in the PhaseState {CurrentPhase} and does not have an ai")
    };
    
    #endregion

    #region Attacks

    private enum Attack
    {
        Teleport,
        IchorTriple,
        SummonCreeper
    }
    
    private Attack[] angerOneAp =
    {
        Attack.Teleport,
        Attack.IchorTriple
    };

    private Attack[] angerTwoAp =
    {
        Attack.Teleport,
        Attack.IchorTriple,
        Attack.IchorTriple,
        Attack.Teleport,
        Attack.SummonCreeper
    };

    private Attack[] angerThreeAp =
    {
        Attack.Teleport,
        Attack.IchorTriple,
        Attack.SummonCreeper,
        Attack.Teleport,
        Attack.IchorTriple,
        Attack.IchorTriple
    };
    
    private Attack[] CurrentAttackPattern => CurrentPhase switch
    {
        PhaseState.AngerOne => angerOneAp,
        PhaseState.AngerTwo => angerTwoAp,
        PhaseState.AngerThree => angerThreeAp,
        _ => throw new UsageException(
            $"BoC is in the PhaseState {CurrentPhase} and does not have an attack pattern")
    };

    private int CurrentAttackIndex
    {
        get => (int) Npc.ai[2];
        set => Npc.ai[2] = value;
    }

    private Attack CurrentAttack => CurrentAttackPattern[CurrentAttackIndex];
    
    private void NextAttack()
    {
        CurrentAttackIndex = (CurrentAttackIndex + 1) % CurrentAttackPattern.Length;
        if(CurrentAttack == 0) Npc.TargetClosest();
    }

    #endregion

    #region AI
    
    private bool countUpTimer = false;

    private bool isBrainOpen = false;

    private bool showPhantoms = false;

    private bool isFleeing = false;
    
    private int AiTimer
    {
        get => (int) Npc.ai[0];
        set => Npc.ai[0] = value;
    }

    public override void OnFirstFrame(NPC npc)
    {
        NPC.crimsonBoss = npc.whoAmI;
        CurrentPhase = PhaseState.Intro;
        AiTimer = 0;
        CloseBrain();
    }

    public override bool AcidAI(NPC npc)
    {
        if (AiTimer > 0 && !countUpTimer)
            AiTimer--;

        // Flee when no players are alive or out of crimson
        var target = Main.player[npc.target];
        if ((IsTargetGone(npc) || !target.ZoneCrimson) && !isFleeing)
        {
            npc.TargetClosest();
            target = Main.player[npc.target];
            if (IsTargetGone(npc) || !target.ZoneCrimson)
            {
                countUpTimer = true;
                isFleeing = true;
                AiTimer = 0;
            }
        }

        if (isFleeing) FleeAI();
        else CurrentAi.Invoke();

        if (countUpTimer)
            AiTimer++;

        return false;
    }
    
    private void FleeAI()
    {
        countUpTimer = true;

        var target = Main.player[Npc.target];
        if (!IsTargetGone(Npc) && target.ZoneCrimson)
        {
            countUpTimer = false;
            AiTimer = 0;
            isFleeing = false;
            return;
        }

        if (AiTimer < 120)
        {
            Npc.velocity.Y += AiTimer * 0.025f;
        }
        else
        {
            Npc.active = false;
            EffectsManager.BossRageKill();
            EffectsManager.ShockwaveKill();
        }
    }

    #region Phase AIs
    
    private void Phase_Intro()
    {
        countUpTimer = true;
        BossBar.MaxCreepers = 10;

        // 10 creepers
        if (AiTimer % 6 == 0 && AiTimer < 60) Attack_SummonCreeper(CreeperOverride.AttackType.Dash);

        if (AiTimer >= 60)
        {
            SoundEngine.PlaySound(SoundID.Roar);

            CurrentPhase = PhaseState.CreeperOne;
            countUpTimer = false;
            AiTimer = 0;
        }
    }

    private void Phase_CreeperOne()
    {
        var creepersAlive = Main.npc.Count(n => n.type == NPCID.Creeper && n.active);

        if (creepersAlive <= 0)
        {
            AiTimer = 0;
            OpenBrain();
            CurrentPhase = PhaseState.AngerOne;
            return;
        }

        // Slow boi
        Attack_HoverToPlayer(0.5f);
    }

    private void Phase_AngerOne()
    {
        if (Npc.GetLifePercent() <= 0.6f && !countUpTimer)
        {
            ResetExtraAI();
            CurrentPhase = PhaseState.TransitionOne;
            CurrentAttackIndex = 0;
            AiTimer = 0;
            return;
        }

        if (AiTimer > 0 && !countUpTimer)
        {
            Attack_HoverToPlayer(1.25f);
            return;
        }

        bool isDone = false;
        switch (CurrentAttack)
        {
            case Attack.Teleport:
                Attack_Teleport(out isDone, 1.5f);
                if (isDone) AiTimer = 120;
                break;
            case Attack.IchorTriple:
                Attack_TripleIchorShot();
                AiTimer = 120;
                NextAttack();
                break;
        }

        if (isDone) NextAttack();
    }

    private void Phase_TransitionOne()
    {
        BossBar.MaxCreepers = 10;
        countUpTimer = true;

        if (AiTimer == 0)
        {
            CloseBrain();
            SoundEngine.PlaySound(SoundID.Roar, Npc.Center);
        }

        if (AiTimer % 6 == 0 && AiTimer < 60) Attack_SummonCreeper(CreeperOverride.AttackType.SuperDash);
        if (AiTimer >= 60)
        {
            ExtraAI[0] = 1;
            countUpTimer = false;
            AiTimer = 60;
            CurrentPhase = PhaseState.CreeperTwo;
        }
    }

    private void Phase_CreeperTwo()
    {
        var creepersAlive = Main.npc.Count(n => n.type == NPCID.Creeper && n.active);

        if (creepersAlive <= 0)
        {
            Npc.dontTakeDamage = false;
            AiTimer = 0;
            OpenBrain();
            CurrentPhase = PhaseState.AngerTwo;
            return;
        }

        if (AiTimer > 0 && !countUpTimer)
        {
            Attack_HoverToPlayer(1f);
        }
        else
        {
            Attack_Teleport(out var isDone, 1f);
            if (isDone) AiTimer = 160;
        }
    }

    private void Phase_AngerTwo()
    {
        if (Npc.GetLifePercent() <= 0.25f && !countUpTimer)
        {
            ResetExtraAI();
            CurrentPhase = PhaseState.TransitionTwo;
            CurrentAttackIndex = 0;
            AiTimer = 0;
            return;
        }
        
        if (AiTimer > 0 && !countUpTimer)
        {
            Attack_HoverToPlayer(1.75f);
            return;
        }

        var isDone = false;
        switch (CurrentAttack)
        {
            case Attack.Teleport:
                Attack_Teleport(out isDone, 2f);
                if (isDone) AiTimer = 120;
                break;
            case Attack.IchorTriple:
                Attack_TripleIchorShot();
                AiTimer = 90;
                isDone = true;
                break;
            case Attack.SummonCreeper:
                BossBar.MaxCreepers = 0; // No Shield
                Attack_SummonCreeper(CreeperOverride.AttackType.Dash);
                AiTimer = 90;
                isDone = true;
                break;
        }

        if (isDone) NextAttack();
    }

    private void Phase_TransitionTwo()
    {
        BossBar.MaxCreepers = 0;
        countUpTimer = true;

        if (AiTimer == 0)
        {
            SoundEngine.PlaySound(SoundID.Roar, Npc.Center);
            EffectsManager.ShockwaveActive(Npc.Center, 0.075f, 0.15f, Color.Transparent);
            var punch = new PunchCameraModifier(Npc.Center, Main.rand.NextVector2Unit(), 10f, 12f, 60, 1000f, FullName);
            Main.instance.CameraModifiers.Add(punch);
            showPhantoms = true;
            
            Npc.velocity = Vector2.Zero;
        }
        // Shockwave
        else if (AiTimer < 120)
        {
            var shockT = AiTimer / 120f;
            EffectsManager.ShockwaveProgress(shockT);
            ConfusePlayers();
        }
        else
        {
            EffectsManager.ShockwaveKill();
            
            countUpTimer = false;
            AiTimer = 60;
            CurrentAttackIndex = 0;
            CurrentPhase = PhaseState.AngerThree;
        }
    }

    private void Phase_AngerThree()
    {
        ConfusePlayers();
        
        if (AiTimer > 0 && !countUpTimer)
        {
            Attack_HoverToPlayer(1f);
            return;
        }

        var isDone = false;
        switch (CurrentAttack)
        {
            case Attack.Teleport:
                Attack_Teleport(out isDone, 2f);
                if (isDone) AiTimer = 90;
                break;
            case Attack.IchorTriple:
                Attack_TripleIchorShot();
                AiTimer = 60;
                isDone = true;
                break;
            case Attack.SummonCreeper:
                BossBar.MaxCreepers = 0; // No Shield
                Attack_SummonCreeper(CreeperOverride.AttackType.Dash);
                AiTimer = 90;
                isDone = true;
                break;
        }

        if (isDone) NextAttack();
    }

    #endregion

    #region Attack Behaviors

    private void Attack_HoverToPlayer(float speed)
    {
        var target = Main.player[Npc.target].Center;
        var direction = Npc.Center.DirectionTo(target);
        Npc.SimpleFlyMovement(direction * speed * MathF.Sqrt(5 * Npc.Distance(target)) / 10f, 0.05f);
    }

    private void Attack_SummonCreeper(CreeperOverride.AttackType type)
    {
        SoundEngine.PlaySound(SoundID.NPCHit9, Npc.Center);

        if (Main.netMode != NetmodeID.MultiplayerClient)
        {
            var pos = Npc.Center + Main.rand.NextVector2Circular(250, 250);
            NPC.NewNPCDirect(Npc.GetSource_FromAI(), pos, NPCID.Creeper, ai1: (int) type);
        }
    }

    private void Attack_TripleIchorShot()
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
    }

    private void Attack_Teleport(out bool isDone, float hoverSpeed)
    {
        const int fadeTime = 45;
        ref var offsetX = ref ExtraAI[0];
        ref var offsetY = ref ExtraAI[1];

        countUpTimer = true;
        isDone = false;

        if (AiTimer == 0 && Main.netMode != NetmodeID.MultiplayerClient)
        {
            var target = Main.player[Npc.target].Center;

            var distance = MathF.Min(Npc.Distance(target), 750); // Don't teleport too far
            distance = MathF.Max(distance, 250); // Nor too close
            
            var pos = Main.rand.NextVector2Unit() * distance;
            offsetX = pos.X;
            offsetY = pos.Y;
            NetSync(Npc);
        }

        switch (AiTimer)
        {
            // Fade out
            case < fadeTime:
            {
                Attack_HoverToPlayer(hoverSpeed);

                var fadeT = EasingHelper.QuadOut((float) AiTimer / fadeTime);
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

                var fadeT = EasingHelper.QuadIn((float) (AiTimer - fadeTime) / fadeTime);
                Npc.Opacity = fadeT;
                break;
            }
            // Done
            case >= fadeTime * 2:
            {
                isDone = true;
                countUpTimer = false;
                Npc.Opacity = 1f;
                ResetExtraAI();
                break;
            }
        }
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
}