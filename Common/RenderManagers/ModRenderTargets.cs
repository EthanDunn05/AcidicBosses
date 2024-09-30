using AcidicBosses.Common.Effects;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace AcidicBosses.Common.RenderManagers;

public class ModRenderTargets : ModSystem
{
    public static ShadedRenderTarget ProjectileBloom;
    
    public override void Load()
    {
        Main.QueueMainThreadAction(() =>
        {
            ProjectileBloom = new ShadedRenderTarget(true, ManagedRenderTarget.CreateScreenSizedTarget, 
                rt => EffectsManager.BloomActivate(rt), RenderLayer.Projectile);
        });
    }
}