using AcidicBosses.Common.Textures;
using AcidicBosses.Content.Bosses.Twins.Projectiles;
using AcidicBosses.Content.ProjectileBases;
using AcidicBosses.Content.Projectiles;
using AcidicBosses.Core.Graphics.Sprites;
using AcidicBosses.Helpers;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AcidicBosses.Content.Bosses.Twins;

public partial class TwinsController
{
    private void NewDashLine(Twin twin, Vector2 position, float offset, int lifetime)
    {
        new EffectLine(TextureRegistry.InvertedFadingGlowLine, twin.Npc.Center, twin.Npc.rotation + offset, 1000f, 33f, Color.Crimson, lifetime)
        {
            OnUpdate = line =>
            {
                line.Position = twin.Npc.Center;
                line.Rotation = twin.Npc.rotation + offset;
                
                var fadeT = 1f - line.LifetimeRatio;
                var color = Color.Red;
                if (twin.Npc.type == NPCID.Spazmatism) color = Color.Lime;
                color *= EasingHelper.CubicOut(fadeT);
                line.DrawColor = color;
            }
        }.Spawn();
    }
    
    private void NewAfterimage(Twin twin, Vector2 startPos, Vector2 endPos)
    {
        new FakeAfterimage(startPos, endPos, twin.Npc).Spawn();
    }

    private Projectile NewSpazFlamethrower(Vector2 pos, float rotation)
    {
        return BaseBetsyFlame.Create<SpazFlamethrower>(NPC.GetSource_FromAI(), pos, rotation - MathHelper.PiOver2, Spazmatism.Npc.damage, 4, Spazmatism.Npc.whoAmI);
    }

    private Projectile NewSpazFireball(Vector2 pos, Vector2 vel)
    {
        return ProjHelper.NewUnscaledProjectile(NPC.GetSource_FromAI(), pos, vel, ProjectileID.CursedFlameHostile,
            Spazmatism.Npc.damage, 3);
    }
    
    private Projectile NewSpazCircleIndicator(Vector2 pos, int lifetime)
    {
        return BaseEffectProjectile.Create<SpazCircleIndicator>(NPC.GetSource_FromAI(), pos, 0f, lifetime);
    }

    private Projectile NewRetDeathray(Vector2 position, float rotation, int lifetime)
    {
        return DeathrayBase.Create<RetDeathray>(NPC.GetSource_FromAI(), position, Retinazer.Npc.damage * 2, 3, rotation,
            lifetime, Retinazer.Npc.whoAmI);
    }

    private Projectile NewRetSweepIndicator(Vector2 pos, float rotation, int lifetime)
    {
        return BaseSweep.Create<RetSweepIndicator>(NPC.GetSource_FromAI(), pos, rotation, lifetime,
            Retinazer.Npc.whoAmI);
    }
    
    private Projectile NewRetLazer(Vector2 pos, Vector2 vel, float rotation, int lifetime, int anchor = -1)
    {
        return BaseLineProjectile.Create<RetLaserIndicator>(NPC.GetSource_FromAI(), pos, vel, Retinazer.Npc.damage, 3, rotation, lifetime, anchor);
    }
}