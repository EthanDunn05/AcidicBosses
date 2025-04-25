using System;
using AcidicBosses.Core.StateManagement;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AcidicBosses.Content.Bosses.QueenSlime;

public partial class QueenSlime : AcidicNPCOverride
{
    protected override int OverriddenNpc => NPCID.QueenSlimeBoss;
    protected override bool BossEnabled => true;

    private Vector2 oldVel = Vector2.UnitY;
    private Vector2 oldOldVel = Vector2.UnitY;
    private PhaseTracker phaseTracker;
    private bool upright = true;

    public override void OnFirstFrame(NPC npc)
    {
        phaseTracker = new PhaseTracker([
            PhaseIntro,
            PhaseOne,
            PhaseTwo,
        ]);
    }

    public override bool AcidAI(NPC npc)
    {
        Npc.MaxFallSpeedMultiplier *= 2f;
        Npc.GravityMultiplier *= 2f;
        
        if (!Npc.HasValidTarget) Npc.TargetClosest();
        
        if ((!grounded || bouncing) && Npc.velocity.Y == 0f && oldVel.Y >= 0 && !Npc.noGravity)
        {
            grounded = true;
            bouncing = false;
            Npc.velocity = Vector2.Zero;
            OnLand(oldOldVel.Y);
        }

        phaseTracker.RunPhaseAI();

        var lerp = Utils.GetLerpValue(-75, 75, Npc.velocity.X, true);
        var goal = MathHelper.Lerp(-MathHelper.PiOver4, MathHelper.PiOver4, lerp);
        Npc.rotation = MathHelper.Lerp(Npc.rotation, goal, 0.5f);
        
        squash = MathHelper.Lerp(squash, 0f, 0.1f);

        upright = true;
        oldOldVel = oldVel;
        oldVel = Npc.velocity;
        return false;
    }
    
    // Stolen from Infernum
    // https://github.com/InfernumTeam/InfernumMode/blob/master/Content/BehaviorOverrides/BossAIs/QueenSlime/QueenSlimeBehaviorOverride.cs#L1223
    public bool OnSolidGround()
    {
        var solidGround = false;
        for (var i = -8; i < 8; i++)
        {
            var ground = Framing.GetTileSafely((int)(Npc.Bottom.X / 16f) + i, (int)(Npc.Bottom.Y / 16f) + 1);
            var notAFuckingTree = ground.TileType is not TileID.Trees and not TileID.PineTree and not TileID.PalmTree;
            if (ground.HasUnactuatedTile && notAFuckingTree && (Main.tileSolid[ground.TileType] || Main.tileSolidTop[ground.TileType]))
            {
                solidGround = true;
                break;
            }
        }
        return solidGround;
    }
    
    
}