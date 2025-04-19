using System;
using AcidicBosses.Common.Configs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace AcidicBosses.Content.Bosses.WoF;

/// <summary>
/// This is for managing hooks that are outside the control of a GlobalNPC
/// I need to use this to untangle the spaghetti that is the WoF code.
/// This doesn't work perfectly, but it works good enough
/// </summary>
public class WoFSystem : ModSystem
{
	private NPC WoF => Main.npc[Main.wofNPCIndex];
	private float WallDistance => WoF.ai[3];

	public static float WofDrawAreaBottomLeft = -1;
	public static float WofDrawAreaBottomRight = -1;
	public static float WofDrawAreaTopLeft = -1;
	public static float WofDrawAreaTopRight = -1;
    
    public override void Load()
    {
        On_Main.DrawWoF += DrawWofBody;
        On_Player.WOFTongue += WoFTongue;
        On_Main.DrawWOFTongueToPlayer += DrawWOFTongueToPlayer;
    }

    public override void Unload()
    {
        On_Main.DrawWoF -= DrawWofBody;
        On_Player.WOFTongue -= WoFTongue;
        On_Main.DrawWOFTongueToPlayer -= DrawWOFTongueToPlayer;
    }
    
    private void WoFTongue(On_Player.orig_WOFTongue orig, Player self)
    {
	    if (!BossToggleConfig.Get().EnableWallOfFlesh || AcidicBosses.DisableReworks())
	    {
		    orig(self);
		    return;
	    }
	    
	    // Adapted from vanilla
        if (Main.wofNPCIndex < 0 || !Main.npc[Main.wofNPCIndex].active)
		{
			return;
		}
        
		var leftWallX = WoF.Center.X - WallDistance - 80;
		var rightWallX = WoF.Center.X + WallDistance;

		if (!self.gross && self.position.Y > (float)((Main.maxTilesY - 250) * 16) && self.position.X > leftWallX - 1920f && self.position.X < rightWallX + 1920f)
		{
			self.AddBuff(37, 10);
			SoundEngine.PlaySound(SoundID.NPCDeath10, WoF.Center);
		}
		
		// Take damage from the wall
		if (self.position.X + self.width > leftWallX && self.position.X < leftWallX + 140f && self.gross)
		{
			self.noKnockback = false;
			var attackDamage_ScaledByStrength = WoF.GetAttackDamage_ScaledByStrength(50f);
			self.Hurt(PlayerDeathReason.LegacyDefault(), attackDamage_ScaledByStrength, 1);
		}
		if (self.position.X + self.width > rightWallX && self.position.X < rightWallX + 140f && self.gross)
		{
			self.noKnockback = false;
			var attackDamage_ScaledByStrength = WoF.GetAttackDamage_ScaledByStrength(50f);
			self.Hurt(PlayerDeathReason.LegacyDefault(), attackDamage_ScaledByStrength, -1);
		}

		if (self.gross && (self.Center.X > rightWallX || self.Center.X < leftWallX))
		{
			self.AddBuff(ModContent.BuffType<NewTonguedBuff>(), 10);
		}
		if (!self.tongued)
		{
			return;
		}
		
		self.controlHook = false;
		self.controlUseItem = false;
		for (int i = 0; i < 1000; i++)
		{
			if (Main.projectile[i].active && Main.projectile[i].owner == Main.myPlayer && Main.projectile[i].aiStyle == 7)
			{
				Main.projectile[i].Kill();
			}
		}
    }

    private void DrawWofBody(On_Main.orig_DrawWoF orig, Main self)
    {
	    if (!BossToggleConfig.Get().EnableWallOfFlesh || AcidicBosses.DisableReworks())
	    {
		    orig(self);
		    return;
	    }
	    
	    if (Main.wofNPCIndex < 0 || !Main.npc[Main.wofNPCIndex].active || Main.npc[Main.wofNPCIndex].life <= 0)
            return;

        // Ripped straight from vanilla code
        var frameHeight = TextureAssets.Wof.Height() / 3;
        
        var drawAreaBottom = Main.screenPosition.Y + Main.screenHeight;
        var drawAreaTopRight = WofDrawAreaTopRight;
        var drawAreaTopLeft = WofDrawAreaTopLeft;
        
        var topRight = (int) ((drawAreaTopRight - Main.screenPosition.Y) / frameHeight) + 1;
        var topLeft = (int) ((drawAreaTopLeft - Main.screenPosition.Y) / frameHeight) + 1;

        if (topRight > 12f || topLeft > 12f)
        {
            return;
        }

        float rightHeight = topRight * frameHeight;
        if (rightHeight > 0f)
        {
            drawAreaTopRight -= rightHeight;
        }
        
        float leftHeight = topLeft * frameHeight;
        if (leftHeight > 0f)
        {
	        drawAreaTopLeft -= leftHeight;
        }

        var leftWallX = WoF.position.X - WallDistance - 80;
        var rightWallX = WoF.position.X + WallDistance;

        int frameY = Main.wofDrawFrameIndex / 6 * frameHeight;
        if (!Main.gamePaused && Main.wofDrawFrameIndex >= 18)
        {
	        Main.wofDrawFrameIndex = 0;
        }

        // Draw Right wall
        for (var i = (int) drawAreaTopRight; i < drawAreaBottom; i += frameHeight)
        {
            var drawAreaHeight = drawAreaBottom - i;
            if (drawAreaHeight > frameHeight)
            {
                drawAreaHeight = frameHeight;
            }

            for (var j = 0; j < drawAreaHeight; j += 16)
            {
                var pos = new Vector2(rightWallX, i + j) - Main.screenPosition;
                var frame = new Rectangle(0, frameY + j, TextureAssets.Wof.Width(), 16);
                
                var y = (i + j) / 16;
                var x = (int) (rightWallX + TextureAssets.Wof.Width() / 2f) / 16;
                var color = Lighting.GetColor(x, y);

                Main.spriteBatch.Draw(TextureAssets.Wof.Value,
	                pos, frame,
	                color, 0f, Vector2.Zero,
	                1f, SpriteEffects.None, 0f);
            }
        }
        
        // Draw Left Wall
        for (var i = (int) drawAreaTopLeft; i < drawAreaBottom; i += frameHeight)
        {
	        var drawAreaHeight = drawAreaBottom - i;
	        if (drawAreaHeight > frameHeight)
	        {
		        drawAreaHeight = frameHeight;
	        }

	        for (var j = 0; j < drawAreaHeight; j += 16)
	        {
		        var pos = new Vector2(leftWallX, i + j) - Main.screenPosition;
		        var frame = new Rectangle(0, frameY + j, TextureAssets.Wof.Width(), 16);
                
		        var y = (i + j) / 16;
		        var x = (int) (leftWallX + TextureAssets.Wof.Width() / 2f) / 16;
		        var color = Lighting.GetColor(x, y);

		        Main.spriteBatch.Draw(TextureAssets.Wof.Value,
			        pos, frame,
			        color, 0f, Vector2.Zero,
			        1f, SpriteEffects.FlipHorizontally, 0f);
	        }
        }
    }
    
    private void DrawWOFTongueToPlayer(On_Main.orig_DrawWOFTongueToPlayer orig, int i)
    {
	    if (!BossToggleConfig.Get().EnableWallOfFlesh || AcidicBosses.DisableReworks())
	    {
		    orig(i);
		    return;
	    }
	    
	    var player = Main.player[i];
	    
	    // Target the closer wall
	    var leftWallPos = WoF.Center;
	    leftWallPos.X -= WallDistance + 200;
	    var rightWallPos = WoF.Center;
	    rightWallPos.X += WallDistance - 200;
            
	    var distRightWall = player.Center.Distance(rightWallPos);
	    var distLeftWall = player.Center.Distance(leftWallPos);
	    
	    // Choose the closer wall as the target
	    float targetX;
	    var targetY = WoF.Center.Y;

	    if (distRightWall < distLeftWall) targetX = rightWallPos.X;
	    else targetX = leftWallPos.X;

	    // Just Vanilla Code
	    Vector2 vector = new Vector2(Main.player[i].position.X + (float)Main.player[i].width * 0.5f, Main.player[i].position.Y + (float)Main.player[i].height * 0.5f);
	    float num3 = targetX - vector.X;
	    float num4 = targetY - vector.Y;
	    float rotation = (float)Math.Atan2(num4, num3) - 1.57f;
	    bool flag = true;
	    while (flag)
	    {
		    float num5 = (float)Math.Sqrt(num3 * num3 + num4 * num4);
		    if (num5 < 40f)
		    {
			    flag = false;
			    continue;
		    }
		    num5 = (float)TextureAssets.Chain12.Height() / num5;
		    num3 *= num5;
		    num4 *= num5;
		    vector.X += num3;
		    vector.Y += num4;
		    num3 = targetX - vector.X;
		    num4 = targetY - vector.Y;
		    Color color = Lighting.GetColor((int)vector.X / 16, (int)(vector.Y / 16f));
		    Main.spriteBatch.Draw(TextureAssets.Chain12.Value, 
			    new Vector2(vector.X - Main.screenPosition.X, vector.Y - Main.screenPosition.Y), 
			    new Rectangle(0, 0, TextureAssets.Chain12.Width(), TextureAssets.Chain12.Height()), 
			    color, rotation, 
			    new Vector2((float)TextureAssets.Chain12.Width() * 0.5f, (float)TextureAssets.Chain12.Height() * 0.5f), 
			    1f, SpriteEffects.None, 0f);
	    }
    }
}