using System.Linq;
using AcidicBosses.Common.Effects;
using AcidicBosses.Content.ProjectileBases;
using Luminance.Common.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace AcidicBosses.Content.Projectiles;

public class BlackHole : BaseEffectProjectile
{
    public override void AI()
    {
        EffectsManager.BlackHoleActivate(50, Projectile.position);
    }

    public override void OnKill(int timeLeft)
    {
        EffectsRegistry.BlackHole.Deactivate();
    }
}