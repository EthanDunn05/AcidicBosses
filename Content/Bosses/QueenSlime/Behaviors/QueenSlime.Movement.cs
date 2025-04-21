using System;
using AcidicBosses.Content.Particles.Animated;
using AcidicBosses.Core.Graphics.Sprites;
using Luminance.Common.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Terraria;
using Terraria.Audio;

namespace AcidicBosses.Content.Bosses.QueenSlime;

public partial class QueenSlime
{
    private bool grounded = false;
    private bool bouncing = false;
    
    private void Jump(Vector2 velocity)
    {
        grounded = false;
        singleFlap = true;
        Npc.velocity = velocity;
        squash = MathHelper.Lerp(0f, -0.5f, Utils.GetLerpValue(0f, 25f, velocity.Length(), true));
    }

    private void JumpTo(Vector2 goal, float landTime)
    {
        var relativePos = goal - Npc.Bottom;

        var velX = relativePos.X / landTime;
        var velY = (relativePos.Y / landTime) - (0.5f * Npc.gravity * landTime);

        var vel = new Vector2(velX, velY);
        Jump(vel);
    }

    private void FlyTo(Vector2 goal, float speed, float accel)
    {
        Npc.SimpleFlyMovement(Npc.DirectionTo(goal) * speed, accel);
        upright = false;
    }

    private void Teleport(Vector2 goal)
    {
        new QueenSlimeFakeAfterimage(Npc.Bottom, goal, Npc).Spawn();
        Npc.Bottom = goal;
    }

    private bool Attack_JumpToPlayer(float landTime)
    {
        AttackManager.CountUp = true;
        if (AttackManager.AiTimer == 0)
        {
            JumpTo(TargetPlayer.Center, landTime);
        }

        if (grounded)
        {
            AttackManager.CountUp = false;
            return true;
        }
        
        return false;
    }
    
    private bool Attack_InstantJumpToPlayer(float landTime)
    {
        JumpTo(TargetPlayer.Center, landTime);
        return true;
    }

    private void OnLand(float force)
    {
        var trueForce = force;
        force = MathHelper.Clamp(force, 0f, 25f);
        var forceLerp = force / 25f;
        
        SoundEngine.PlaySound(SlamSound with {Volume = forceLerp}, Npc.Bottom);
        
        // Smoke puffs
        var puff = new WideGroundPuffParticle(Npc.Bottom, Vector2.Zero, 0f, Color.White, 30);
        puff.Scale *= 1.5f;
        puff.Opacity = 0.25f;
        puff.FrameInterval = 4;
        puff.Spawn();
        var puff2 = new WideGroundPuffParticle(Npc.Bottom, Vector2.Zero, 0f, Color.White, 30);
        puff2.Opacity = 0.25f ;
        puff2.Spawn();

        // Dust from tiles landed on
        var xTile = Npc.position.ToTileCoordinates().X;
        var yTile = Npc.Bottom.ToTileCoordinates().Y;
        for (var x = xTile; x < Npc.width / 16f + xTile; x++)
        {
            var ground = new Point(x, yTile);
            WorldGen.KillTile(ground.X, ground.Y, true, true);
        }

        if (forceLerp > 0.25f)
        {
            bouncing = true;
            squash = forceLerp * 0.25f;
            Npc.velocity.Y = -trueForce * 0.25f;
            Npc.velocity.X = oldVel.X * 0.25f;
        }
        else
        {
            Npc.velocity.X = 0f;
        }
    }
}