using System.ComponentModel;
using AcidicBosses.Helpers;
using Luminance.Common.Utilities;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace AcidicBosses.Common.Configs;

public class BossToggleConfig : ModConfig
{
    public static BossToggleConfig Get()
    {
        return ModContent.GetInstance<BossToggleConfig>();
    }
    
    public override ConfigScope Mode => ConfigScope.ServerSide;

    public override bool NeedsReload(ModConfig pendingConfig)
    {
        return Utilities.AnyBosses();
    }

    [Header("ToggleBosses")]
    
    [DefaultValue(true)]
    public bool EnableKingSlime;
    
    [DefaultValue(true)]
    public bool EnableEyeOfCthulhu;
    
    [DefaultValue(true)]
    public bool EnableEaterOfWorlds;
    
    [DefaultValue(true)]
    public bool EnableBrainOfCthulhu;
    
    [DefaultValue(true)]
    public bool EnableSkeletron;
    
    [DefaultValue(true)]
    public bool EnableWallOfFlesh;
}