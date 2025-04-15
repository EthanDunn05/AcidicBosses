using System;
using System.Linq;
using AcidicBosses.Common;
using AcidicBosses.Common.Configs;
using AcidicBosses.Common.Effects;
using AcidicBosses.Core.Systems.DifficultySystem;
using Luminance.Common.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace AcidicBosses.Content.Bosses.QueenBee;

public class BeeModification : GlobalNPC
{
    public override bool AppliesToEntity(NPC entity, bool lateInstantiation)
    {
        return entity.type == NPCID.Bee || entity.type == NPCID.BeeSmall;
    }
    
    public override void SetStaticDefaults()
    {
        base.SetStaticDefaults();
        NPCID.Sets.TrailingMode[NPCID.Bee] = 3;
        NPCID.Sets.TrailingMode[NPCID.BeeSmall] = 3;
    }

    public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color lightColor)
    {
        if (!AcidicDifficultySystem.AcidicActive || !BossToggleConfig.Get().EnableQueenBee) return true;
        if (!Main.npc.Any(n => n.type == NPCID.QueenBee && n.active)) return true;
        
        var drawPos = npc.Center - Main.screenPosition;
        var texture = TextureAssets.Npc[npc.type].Value;
        var origin = npc.frame.Size() * 0.5f;
        var effects = SpriteEffects.None;
        if (npc.spriteDirection > 0) effects = SpriteEffects.FlipHorizontally;
        lightColor *= npc.Opacity;

        // Flash Red
        var pulseTimer = MathF.Sin((float)(Main.timeForVisualEffects / 60f * MathHelper.Pi));
        pulseTimer = Utils.GetLerpValue(-1f, 1f, pulseTimer);
        pulseTimer *= 0.75f;
        pulseTimer += 0.25f;
        
        lightColor = Color.Lerp(lightColor, Color.Red, pulseTimer);
        
        for (var i = 1; i < npc.oldPos.Length; i += 2)
        {
            // All of this is heavily simplified from decompiled vanilla
            var fade = 0.5f * (10 - i) / 20f;
            var afterImageColor = Color.Multiply(lightColor, fade);
        
            var pos = npc.oldPos[i] + new Vector2(npc.width, npc.height) / 2f - Main.screenPosition;
            spriteBatch.Draw(texture, pos, npc.frame, afterImageColor, npc.oldRot[i], origin, npc.scale,
                effects, 0f);
        }
        
        spriteBatch.Draw(
            texture, drawPos,
            npc.frame, lightColor,
            npc.rotation, origin, npc.scale,
            effects, 0f);

        return false;
    }
}