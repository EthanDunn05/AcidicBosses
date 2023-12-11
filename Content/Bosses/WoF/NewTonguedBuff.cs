using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace AcidicBosses.Content.Bosses.WoF;

public class NewTonguedBuff : ModBuff
{
    private NPC WoF => Main.npc[Main.wofNPCIndex];
    private float WallDistance => WoF.ai[3];

    public override string Texture => "Terraria/Images/Buff_38";

    public override void SetStaticDefaults()
    {
        Main.debuff[Type] = true;
        Main.buffNoTimeDisplay[Type] = true;
    }

    public override void Update(Player player, ref int buffIndex)
    {
        player.tongued = true;
        
        // Modify vanilla code to support the second wall
        player.StopVanityActions();
        var done = false;
        
        if (Main.wofNPCIndex >= 0)
        {
            var target = new Vector2(0, WoF.Center.Y);

            // Target in front of the closer wall
            var rightWallPos = WoF.Center;
            rightWallPos.X += WallDistance;
            var leftWallPos = WoF.Center;
            leftWallPos.X -= WallDistance;
            
            var distRightWall = player.Center.Distance(rightWallPos);
            var distLeftWall = player.Center.Distance(leftWallPos);
            
            if (distRightWall < distLeftWall)
            {
                target.X = rightWallPos.X - 200;
            }
            else
            {
                target.X = leftWallPos.X + 200;
            }

            // The rest is vanilla logic with vector math
            var dist = target.Distance(player.Center);
            var releaseDist = 11f;
            
            var closeness = dist;
            if (dist > releaseDist)
            {
                closeness = releaseDist / dist;
            }
            else
            {
                closeness = 1f;
                done = true;
            }
            var vel = (target - player.Center) * closeness;
            player.velocity = vel;
        }
        else
        {
            done = true;
        }
        if (done && Main.myPlayer == player.whoAmI)
        {
            player.ClearBuff(ModContent.BuffType<NewTonguedBuff>());
        }
    }
}