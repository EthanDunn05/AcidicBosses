using System.ComponentModel;
using System.Linq;
using AcidicBosses.Helpers;
using Luminance.Common.Utilities;
using Terraria;
using Terraria.ID;
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
        var pending = (BossToggleConfig)pendingConfig;
        
        if (CheckBoss(NPCID.KingSlime, EnableKingSlime, pending.EnableKingSlime)) return true;
        if (CheckBoss(NPCID.EyeofCthulhu, EnableEyeOfCthulhu, pending.EnableEyeOfCthulhu)) return true;
        if (CheckBoss(NPCID.EaterofWorldsHead, EnableEaterOfWorlds, pending.EnableEaterOfWorlds)) return true;
        if (CheckBoss(NPCID.BrainofCthulhu, EnableBrainOfCthulhu, pending.EnableBrainOfCthulhu)) return true;
        if (CheckBoss(NPCID.QueenBee, EnableQueenBee, pending.EnableQueenBee)) return true;
        if (CheckBoss(NPCID.SkeletronHead, EnableSkeletron, pending.EnableSkeletron)) return true;
        if (CheckBoss(NPCID.WallofFlesh, EnableWallOfFlesh, pending.EnableWallOfFlesh)) return true;
        if (CheckBoss(NPCID.QueenSlimeBoss, EnableQueenSlime, pending.EnableQueenSlime)) return true;
        if (CheckBoss(NPCID.Retinazer, EnableTwins, pending.EnableTwins)) return true;
        if (CheckBoss(NPCID.Spazmatism, EnableTwins, pending.EnableTwins)) return true;
        
        return false;
    }

    private bool CheckBoss(short npcid, bool current, bool pending)
    {
        return current != pending && Main.npc.Any(n => n.active && n.type == npcid);
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
    public bool EnableQueenBee;
    
    [DefaultValue(true)]
    public bool EnableSkeletron;
    
    [DefaultValue(true)]
    public bool EnableWallOfFlesh;
    
    [DefaultValue(true)] 
    public bool EnableQueenSlime;

    [DefaultValue(true)] 
    public bool EnableTwins;
}