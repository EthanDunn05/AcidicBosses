using AcidicBosses.Common.Textures;
using AcidicBosses.Core.Graphics.Sprites;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace AcidicBosses.Content.Bosses.QueenSlime;

public class BouncySlimeOverride : AcidicNPCOverride
{
    protected override int OverriddenNpc => NPCID.QueenSlimeMinionPink;
    protected override bool BossEnabled => true;
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
            
            line ??= new FadingEffectLine(
                TextureRegistry.InvertedFadingGlowLine,
                Npc.Center,
                Npc.DirectionTo(TargetPlayer.Center).ToRotation(),
                1000f,
                10f,
                Color.Red,
                30
            )
            {
                OnUpdate = el =>
                {
                    el.Position = Npc.Center;
                    el.Rotation = Npc.DirectionTo(TargetPlayer.Center).ToRotation();
                    el.Length = Npc.Distance(TargetPlayer.Center);
                }
            }.Spawn();

            line.Time = 0;
        }
        else
        {
            delayedThisShoot = false;
            line = null;
        }
        
        return true;
    }
}