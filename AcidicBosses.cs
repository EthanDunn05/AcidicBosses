using System.IO;
using AcidicBosses.Common.Effects;
using AcidicBosses.Content.Bosses;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace AcidicBosses;

/**
 * The Acidic Bosses Mod!
 * 
 * Created by AcidAssassin
 */
partial class AcidicBosses : Mod
{
    public override void Load()
    {
        if (Main.netMode != NetmodeID.Server)
        {
            EffectsRegistry.LoadShaders(Assets);
        }
    }

    
}