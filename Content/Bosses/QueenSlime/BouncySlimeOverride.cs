using System;
using AcidicBosses.Common.Configs;
using AcidicBosses.Common.Textures;
using AcidicBosses.Core.Graphics.Sprites;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace AcidicBosses.Content.Bosses.QueenSlime;

public class BouncySlimeOverride : AcidicNPCOverride
{
    protected override int OverriddenNpc => NPCID.QueenSlimeMinionPink;
    protected override bool BossEnabled => BossToggleConfig.Get().EnableQueenSlime;
    private bool delayedThisShoot = false;
    private EffectLine? line;

    public override bool AcidAI(NPC npc)
    {
        var shootCooldown = 30;
        if (!Main.expertMode) shootCooldown = 40;
        
        // Nerf these awful enemies
        if ((int)Npc.ai[0] == -35)
        {
            if ((int)Npc.localAI[0] == shootCooldown)
            {
                if (!delayedThisShoot)
                {
                    Npc.localAI[0] = shootCooldown * 2;
                    delayedThisShoot = true;
                }
                else
                {
                    delayedThisShoot = false;
                }
            }

            var num24 =  Main.player[Npc.target].Center.X - npc.Center.X;
            var num25 =  Main.player[Npc.target].Center.Y - npc.Center.Y;
            if (Math.Abs(num24) < 500f && Math.Abs(num25) < 550f && Collision.CanHit(npc.position, npc.width,
                    npc.height, Main.player[Npc.target].position, Main.player[Npc.target].width,
                    Main.player[Npc.target].height) && npc.velocity.Y == 0f)
            {
                line ??= new FadingEffectLine(
                    TextureRegistry.InvertedFadingGlowLine,
                    Npc.Center,
                    Npc.DirectionTo(TargetPlayer.Center).ToRotation(),
                    500f,
                    10f,
                    Color.DeepPink,
                    30
                )
                {
                    OnUpdate = el =>
                    {
                        el.Position = Npc.Center;
                        el.Rotation = Npc.DirectionTo(TargetPlayer.Center).ToRotation();
                    }
                }.Spawn();

                line.Time = 0;
            }
            else
            {
                line = null;
            }
        }
        else
        {
            delayedThisShoot = false;
        }
        
        return true;
    }
}