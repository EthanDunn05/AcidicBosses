using System;
using System.Collections.Generic;
using System.Linq;
using AcidicBosses.Common.Configs;
using AcidicBosses.Common.Textures;
using AcidicBosses.Content.Particles.Animated;
using AcidicBosses.Content.ProjectileBases;
using AcidicBosses.Core.Graphics.Sprites;
using AcidicBosses.Helpers;
using Luminance.Common.VerletIntergration;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace AcidicBosses.Content.Bosses.BoC;

public class CreeperOverride : AcidicNPCOverride
{
    protected override int OverriddenNpc => NPCID.Creeper;
    
    protected override bool BossEnabled => BossToggleConfig.Get().EnableBrainOfCthulhu;

    public enum AttackType
    {
        Dash,
        SuperDash
    }

    private Action Ai => CurrentAttackType switch
    {
        AttackType.Dash => DashAI,
        AttackType.SuperDash => SuperDashAI
    };

    private AttackType CurrentAttackType
    {
        get => (AttackType) Npc.ai[1];
        set => Npc.ai[1] = (int) value;
    }
    
    private int AiTimer
    {
        get => (int) Npc.ai[0];
        set => Npc.ai[0] = value;
    }

    private bool useAfterimages = false;

    private bool countUpTimer = false;
    
    // Only exists on client
    private List<VerletSegment> connectionSegments = [];
    private VerletSettings connectionSimSettings = new()
    {
        ConserveEnergy = true
    };
    
    private Texture2D connectionTex = TextureAssets.Chain12.Value; // The sprite is oriented vertically
    private Asset<Texture2D> connectionTexAsset = TextureAssets.Chain12;
    private int connectionLength = 500;

    public override void SetStaticDefaults()
    {
        base.SetStaticDefaults();
        NPCID.Sets.TrailingMode[NPCID.Creeper] = 1;
    }

    public override void SetDefaults(NPC entity)
    {
        base.SetDefaults(entity);
        
        // 75% max health
        entity.lifeMax = (int) (entity.lifeMax * 0.75f);
        entity.life = entity.lifeMax;
    }

    public override void OnFirstFrame(NPC npc)
    {
        npc.TargetClosest();
        
        for (var i = 0; i < 20; i++)
        {
            var vel = Main.rand.NextVector2Circular(5, 5);
            Dust.NewDustDirect(Npc.Center, 0, 0, DustID.Blood, vel.X, vel.Y);
        }

        new RingBurstParticle(Npc.Center, Vector2.Zero, 0f, Color.Red, 30).Spawn();
        
        // Fill out the connector on the client
        if (Main.netMode != NetmodeID.Server) FillTether();
    }

    public override void DrawBehind(NPC npc, int index)
    {
        if (!ShouldOverride()) return;
        
        // Draw over the brain
        Main.instance.DrawCacheNPCProjectiles.Add(index);
    }

    public override bool AcidAI(NPC npc)
    {
        if (!Main.npc.IndexInRange(NPC.crimsonBoss) || !Main.npc[NPC.crimsonBoss].active)
        {
            npc.active = false;
            npc.netUpdate = true;
            return false;
        }
        
        if (AiTimer > 0 && !countUpTimer)
            AiTimer--;

        Ai.Invoke();
        
        if (countUpTimer)
            AiTimer++;
        
        return false;
    }

    public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
    {
        // Empty loot pool
        npcLoot.RemoveWhere(_ => true);
    }
    
    private void FillTether()
    {
        connectionSegments.Clear(); // Just to be safe
        
        for (var i = 0; i < connectionLength; i += connectionTex.Height)
        {
            connectionSegments.Add(
                new VerletSegment(new Vector2(Npc.Center.X + i, Npc.Center.Y), Vector2.Zero));
        }
            
        // Remove one segment
        connectionSegments.RemoveAt(0);
    }

    #region Ai

    private void DashAI()
    {
        ref var attackState = ref Npc.localAI[0];
        
        bool isDone;
        switch ((int) attackState)
        {
            case 0:
                HoverAround(out isDone, 300);
                if (isDone)
                {
                    attackState++;
                    AiTimer = 0;
                }
                break;
            case 1:
                Dash(out isDone);
                if (isDone)
                {
                    attackState++;
                    AiTimer = 0;
                }
                break;
        }

        if (attackState > 1) attackState = 0;
    }

    private void SuperDashAI()
    {
        ref var attackState = ref Npc.localAI[0];

        if (AiTimer > 0 && !countUpTimer)
        {
            Npc.SimpleFlyMovement(Vector2.Zero, 0.5f);
            return;
        }
        
        bool isDone;
        switch ((int) attackState)
        {
            case 0:
                HoverAround(out isDone, 180);
                if (isDone)
                {
                    attackState++;
                    AiTimer = 0;
                }
                break;
            case > 0:
                Dash(out isDone);
                if (isDone)
                {
                    attackState++;
                    AiTimer = 15;
                }
                break;
        }

        // 2 Dashes
        if (attackState > 2) attackState = 0;
    }

    #endregion

    #region Attacks

    private void HoverAround(out bool isDone, int length)
    {
        countUpTimer = true;
        if (AiTimer < length)
        {
            isDone = false;
            var brain = Main.npc[NPC.crimsonBoss];
            
            // Adapted from vanilla
            // Not fully sure how this works, but it's what vanilla uses
            var dist = Npc.Distance(brain.Center);
            if (dist > 90f)
            {
                dist = 8f / dist;
                var offset = brain.Center - Npc.Center;
                offset *= dist;
                
                Npc.velocity = (Npc.velocity * 15f + offset) / 16f;
                return;
            }
            if (Math.Abs(Npc.velocity.X) + Math.Abs(Npc.velocity.Y) < 8f)
            {
                Npc.velocity *= 1.05f;
            }
        }
        else
        {
            isDone = true;
            countUpTimer = false;
        }
    }

    private void Dash(out bool isDone)
    {
        const int dashLength = 30;
        const int dashAtTime = 30;
        const int dashTrackTime = 10;

        countUpTimer = true;
        var target = Main.player[Npc.target].Center;

        if (AiTimer == 0)
        {
            NewDashLine(dashAtTime);
        }

        if (AiTimer == 0)
        {
            LookTowards(target, 1f);
        }
        if (AiTimer < dashTrackTime)
        {
            Npc.SimpleFlyMovement(Vector2.Zero, 0.75f);
            LookTowards(target, 0.25f);
        }
        else if (AiTimer == dashAtTime)
        {
            useAfterimages = true;
            Npc.TargetClosest();
            Npc.velocity = Npc.rotation.ToRotationVector2() * 25f;
        }
        else if (AiTimer >= dashAtTime + dashLength)
        {
            useAfterimages = false;
            isDone = true;
            countUpTimer = false;

            Npc.rotation = 0;
            return;
        }

        isDone = false;
    }
    
    private void NewDashLine(int lifetime)
    {
        new EffectLine(TextureRegistry.InvertedFadingGlowLine, Npc.Center, Npc.rotation, 1000f, 14f, Color.Crimson, lifetime)
        {
            OnUpdate = line =>
            {
                line.Position = Npc.Center;
                line.Rotation = Npc.rotation;
                line.DrawColor = Color.Crimson * EasingHelper.CubicOut(1f - line.LifetimeRatio);
            }
        }.Spawn();
    }
    
    #endregion

    public override bool AcidicDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
    {
        var drawPos = npc.Center - Main.screenPosition;
        var texture = TextureAssets.Npc[npc.type].Value;
        var origin = npc.frame.Size() * 0.5f;
        
        DrawTether();
        
        // Afterimages
        if (useAfterimages)
            for (var i = 1; i < npc.oldPos.Length; i++)
            {
                // All of this is heavily simplified from decompiled vanilla
                var fade = 0.5f * (10 - i) / 20f;
                var afterImageColor = Color.Multiply(drawColor, fade);

                var pos = npc.oldPos[i] + new Vector2(npc.width, npc.height) / 2f - Main.screenPosition;
                spriteBatch.Draw(texture, pos, npc.frame, afterImageColor, 0f, origin, npc.scale,
                    SpriteEffects.None, 0f);
            }

        spriteBatch.Draw(
            texture, drawPos,
            npc.frame, drawColor,
            0f, origin, npc.scale,
            SpriteEffects.None, 0f);

        return false;
    }

    private void DrawTether()
    {
        var brain = Main.npc[NPC.crimsonBoss];
        if (brain == null || !brain.active) return;
        
        connectionSegments = VerletSimulations.RopeVerletSimulation(connectionSegments, brain.Center,
            connectionLength * 0.75f, connectionSimSettings, Npc.Center);
        
        var renderSettings = new PrimitiveSettings(
            _ => connectionTex.Width / 2f,
            p =>
            {
                var index = (int) (p * connectionSegments.Count);
                if (index == connectionSegments.Count) index--;
                var color = Lighting.GetColor(connectionSegments[index].Position.ToTileCoordinates());
                color *= brain.Opacity;
                return color;
            },
            Shader: ShaderManager.GetShader("AcidicBosses.Rope")
        );
        renderSettings.Shader.SetTexture(connectionTexAsset, 1, SamplerState.PointClamp);
        renderSettings.Shader.TrySetParameter("segments", connectionSegments.Count);
    
        PrimitiveRenderer.RenderTrail(connectionSegments.Select(s => s.Position), renderSettings);
    }
}