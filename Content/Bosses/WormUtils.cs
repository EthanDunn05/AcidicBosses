using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;

namespace AcidicBosses.Content.Bosses;

/// <summary>
/// Heavily based off of <see cref="https://github.com/tModLoader/tModLoader/blob/1.4.4/ExampleMod/Content/NPCs/Worm.cs">Worm.cs</see>
/// </summary>
public static class WormUtils
{
    private static int SpawnSegment(NPC npc, IEntitySource source, int type, int latestNPC)
    {
        // We spawn a new NPC, setting latestNPC to the newer NPC, whilst also using that same variable
        // to set the parent of this new NPC. The parent of the new NPC (may it be a tail or body part)
        // will determine the movement of this new NPC.
        // Under there, we also set the realLife value of the new NPC, because of what is explained above.
        int oldLatest = latestNPC;
        latestNPC = NPC.NewNPC(source, (int) npc.Center.X, (int) npc.Center.Y, type, npc.whoAmI, 0, latestNPC);

        Main.npc[oldLatest].ai[0] = latestNPC;

        NPC latest = Main.npc[latestNPC];
        // NPC.realLife is the whoAmI of the NPC that the spawned NPC will share its health with
        latest.realLife = npc.whoAmI;

        return latestNPC;
    }

    public static void HeadSpawnSegments(NPC npc, int length, int headType, int bodyType, int tailType)
    {
        if (Main.netMode != NetmodeID.MultiplayerClient)
        {
            // So, we start the AI off by checking if NPC.ai[0] (the following NPC's whoAmI) is 0.
            // This is practically ALWAYS the case with a freshly spawned NPC, so this means this is the first update.
            // Since this is the first update, we can safely assume we need to spawn the rest of the worm (bodies + tail).
            bool hasFollower = npc.ai[0] > 0;
            if (!hasFollower)
            {
                // So, here we assign the NPC.realLife value.
                // The NPC.realLife value is mainly used to determine which NPC loses life when we hit this NPC.
                // We don't want every single piece of the worm to have its own HP pool, so this is a neat way to fix that.
                npc.realLife = npc.whoAmI;
                // latestNPC is going to be used in SpawnSegment() and I'll explain it there.
                int latestNPC = npc.whoAmI;

                // Here we determine the length of the worm.
                int randomWormLength = length;

                int distance = randomWormLength - 2;

                IEntitySource source = npc.GetSource_FromAI();
                
                // Spawn the body segments like usual
                while (distance > 0)
                {
                    latestNPC = SpawnSegment(npc, source, bodyType, latestNPC);
                    distance--;
                }

                // Spawn the tail segment
                SpawnSegment(npc, source, tailType, latestNPC);

                npc.netUpdate = true;

                // Ensure that all of the segments could spawn.  If they could not, despawn the worm entirely
                int count = 0;
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC n = Main.npc[i];

                    if (n.active && (n.type == headType || n.type == bodyType || n.type == tailType) &&
                        n.realLife == npc.whoAmI)
                        count++;
                }

                if (count != randomWormLength)
                {
                    // Unable to spawn all of the segments... kill the worm
                    for (int i = 0; i < Main.maxNPCs; i++)
                    {
                        NPC n = Main.npc[i];

                        if (n.active && (n.type == headType || n.type == bodyType || n.type == tailType) &&
                            n.realLife == npc.whoAmI)
                        {
                            n.active = false;
                            n.netUpdate = true;
                        }
                    }
                }

                // Set the player target for good measure
                npc.TargetClosest(true);
            }
        }
    }

    public static void HeadDigAI(NPC npc, float speed, float acceleration, Vector2? targetPos, bool canFly = false)
    {
        bool collision = CheckCollision(npc, true);

        HeadAI_CheckTargetDistance(npc, ref collision, 1000, targetPos);

        HeadAI_Movement(npc, collision, speed, acceleration, targetPos, canFly);
    }

    public static bool CheckCollision(NPC npc, bool useDigEffect)
    {
        int minTilePosX = (int) (npc.Left.X / 16) - 1;
        int maxTilePosX = (int) (npc.Right.X / 16) + 2;
        int minTilePosY = (int) (npc.Top.Y / 16) - 1;
        int maxTilePosY = (int) (npc.Bottom.Y / 16) + 2;

        // Ensure that the tile range is within the world bounds
        if (minTilePosX < 0)
            minTilePosX = 0;
        if (maxTilePosX > Main.maxTilesX)
            maxTilePosX = Main.maxTilesX;
        if (minTilePosY < 0)
            minTilePosY = 0;
        if (maxTilePosY > Main.maxTilesY)
            maxTilePosY = Main.maxTilesY;

        bool collision = false;

        // This is the initial check for collision with tiles.
        for (int i = minTilePosX; i < maxTilePosX; ++i)
        {
            for (int j = minTilePosY; j < maxTilePosY; ++j)
            {
                Tile tile = Main.tile[i, j];

                // If the tile is solid or is considered a platform, then there's valid collision
                if (tile.HasUnactuatedTile && (Main.tileSolid[tile.TileType] ||
                                               Main.tileSolidTop[tile.TileType] && tile.TileFrameY == 0) ||
                    tile.LiquidAmount > 64)
                {
                    Vector2 tileWorld = new Point16(i, j).ToWorldCoordinates(0, 0);

                    if (npc.Right.X > tileWorld.X && npc.Left.X < tileWorld.X + 16 && npc.Bottom.Y > tileWorld.Y &&
                        npc.Top.Y < tileWorld.Y + 16)
                    {
                        // Collision found
                        collision = true;

                        if (useDigEffect)
                        {
                            if (Main.rand.NextBool(100))
                                WorldGen.KillTile(i, j, fail: true, effectOnly: true, noItem: false);
                        }
                    }
                }
            }
        }

        return collision;
    }

    private static void HeadAI_CheckTargetDistance(NPC npc, ref bool collision, int maxDistance,
        Vector2? forcedTargetPosition)
    {
        // If there is no collision with tiles, we check if the distance between this NPC and its target is too large, so that we can still trigger "collision".
        if (!collision)
        {
            Rectangle hitbox = npc.Hitbox;

            bool tooFar = true;

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Rectangle areaCheck;

                Player player = Main.player[i];

                if (forcedTargetPosition is Vector2 target)
                    areaCheck = new Rectangle((int) target.X - maxDistance, (int) target.Y - maxDistance,
                        maxDistance * 2, maxDistance * 2);
                else if (player.active && !player.dead && !player.ghost)
                    areaCheck = new Rectangle((int) player.position.X - maxDistance,
                        (int) player.position.Y - maxDistance, maxDistance * 2, maxDistance * 2);
                else
                    continue; // Not a valid player

                if (hitbox.Intersects(areaCheck))
                {
                    tooFar = false;
                    break;
                }
            }

            if (tooFar)
                collision = true;
        }
    }

    private static void HeadAI_Movement(NPC npc, bool collision, float speed, float acceleration,
        Vector2? forcedTargetPosition, bool canFly = false)
    {
        float targetXPos, targetYPos;

        Player playerTarget = Main.player[npc.target];

        Vector2 forcedTarget = forcedTargetPosition ?? playerTarget.Center;
        // Using a ValueTuple like this allows for easy assignment of multiple values
        (targetXPos, targetYPos) = (forcedTarget.X, forcedTarget.Y);

        // Copy the value, since it will be clobbered later
        Vector2 npcCenter = npc.Center;

        float targetRoundedPosX = (float) ((int) (targetXPos / 16f) * 16);
        float targetRoundedPosY = (float) ((int) (targetYPos / 16f) * 16);
        npcCenter.X = (float) ((int) (npcCenter.X / 16f) * 16);
        npcCenter.Y = (float) ((int) (npcCenter.Y / 16f) * 16);
        float dirX = targetRoundedPosX - npcCenter.X;
        float dirY = targetRoundedPosY - npcCenter.Y;

        float length = (float) Math.Sqrt(dirX * dirX + dirY * dirY);

        // If we do not have any type of collision, we want the NPC to fall down and de-accelerate along the X axis.
        if (!collision && !canFly)
            HeadAI_Movement_HandleFallingFromNoCollision(npc, dirX, speed, acceleration);
        else
        {
            // Else we want to play some audio (soundDelay) and move towards our target.
            HeadAI_Movement_PlayDigSounds(npc, length);

            HeadAI_Movement_HandleMovement(npc, dirX, dirY, length, speed, acceleration);
        }

        HeadAI_Movement_SetRotation(npc, collision);
    }

    private static void HeadAI_Movement_HandleFallingFromNoCollision(NPC npc, float dirX, float speed,
        float acceleration)
    {
        // Keep searching for a new target
        npc.TargetClosest(true);

        // Constant gravity of 0.11 pixels/tick
        npc.velocity.Y += 0.11f;

        // Ensure that the NPC does not fall too quickly
        if (npc.velocity.Y > speed)
            npc.velocity.Y = speed;

        // The following behavior mimics vanilla worm movement
        if (Math.Abs(npc.velocity.X) + Math.Abs(npc.velocity.Y) < speed * 0.4f)
        {
            // Velocity is sufficiently fast, but not too fast
            if (npc.velocity.X < 0.0f)
                npc.velocity.X -= acceleration * 1.1f;
            else
                npc.velocity.X += acceleration * 1.1f;
        }
        else if (npc.velocity.Y == speed)
        {
            // NPC has reached terminal velocity
            if (npc.velocity.X < dirX)
                npc.velocity.X += acceleration;
            else if (npc.velocity.X > dirX)
                npc.velocity.X -= acceleration;
        }
        else if (npc.velocity.Y > 4)
        {
            if (npc.velocity.X < 0)
                npc.velocity.X += acceleration * 0.9f;
            else
                npc.velocity.X -= acceleration * 0.9f;
        }
    }

    private static void HeadAI_Movement_PlayDigSounds(NPC npc, float length)
    {
        if (npc.soundDelay == 0)
        {
            // Play sounds quicker the closer the NPC is to the target location
            float num1 = length / 40f;

            if (num1 < 10)
                num1 = 10f;

            if (num1 > 20)
                num1 = 20f;

            npc.soundDelay = (int) num1;

            SoundEngine.PlaySound(SoundID.WormDig, npc.position);
        }
    }

    private static void HeadAI_Movement_HandleMovement(NPC npc, float dirX, float dirY, float length, float speed,
        float acceleration)
    {
        float absDirX = Math.Abs(dirX);
        float absDirY = Math.Abs(dirY);
        float newSpeed = speed / length;
        dirX *= newSpeed;
        dirY *= newSpeed;

        if ((npc.velocity.X > 0 && dirX > 0) || (npc.velocity.X < 0 && dirX < 0) || (npc.velocity.Y > 0 && dirY > 0) ||
            (npc.velocity.Y < 0 && dirY < 0))
        {
            // The NPC is moving towards the target location
            if (npc.velocity.X < dirX)
                npc.velocity.X += acceleration;
            else if (npc.velocity.X > dirX)
                npc.velocity.X -= acceleration;

            if (npc.velocity.Y < dirY)
                npc.velocity.Y += acceleration;
            else if (npc.velocity.Y > dirY)
                npc.velocity.Y -= acceleration;

            // The intended Y-velocity is small AND the NPC is moving to the left and the target is to the right of the NPC or vice versa
            if (Math.Abs(dirY) < speed * 0.2 && ((npc.velocity.X > 0 && dirX < 0) || (npc.velocity.X < 0 && dirX > 0)))
            {
                if (npc.velocity.Y > 0)
                    npc.velocity.Y += acceleration * 2f;
                else
                    npc.velocity.Y -= acceleration * 2f;
            }

            // The intended X-velocity is small AND the NPC is moving up/down and the target is below/above the NPC
            if (Math.Abs(dirX) < speed * 0.2 && ((npc.velocity.Y > 0 && dirY < 0) || (npc.velocity.Y < 0 && dirY > 0)))
            {
                if (npc.velocity.X > 0)
                    npc.velocity.X = npc.velocity.X + acceleration * 2f;
                else
                    npc.velocity.X = npc.velocity.X - acceleration * 2f;
            }
        }
        else if (absDirX > absDirY)
        {
            // The X distance is larger than the Y distance.  Force movement along the X-axis to be stronger
            if (npc.velocity.X < dirX)
                npc.velocity.X += acceleration * 1.1f;
            else if (npc.velocity.X > dirX)
                npc.velocity.X -= acceleration * 1.1f;

            if (Math.Abs(npc.velocity.X) + Math.Abs(npc.velocity.Y) < speed * 0.5)
            {
                if (npc.velocity.Y > 0)
                    npc.velocity.Y += acceleration;
                else
                    npc.velocity.Y -= acceleration;
            }
        }
        else
        {
            // The X distance is larger than the Y distance.  Force movement along the X-axis to be stronger
            if (npc.velocity.Y < dirY)
                npc.velocity.Y += acceleration * 1.1f;
            else if (npc.velocity.Y > dirY)
                npc.velocity.Y -= acceleration * 1.1f;

            if (Math.Abs(npc.velocity.X) + Math.Abs(npc.velocity.Y) < speed * 0.5)
            {
                if (npc.velocity.X > 0)
                    npc.velocity.X += acceleration;
                else
                    npc.velocity.X -= acceleration;
            }
        }
    }

    private static void HeadAI_Movement_SetRotation(NPC npc, bool collision)
    {
        // Set the correct rotation for this NPC.
        // Assumes the sprite for the NPC points upward.  You might have to modify this line to properly account for your NPC's orientation
        npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;

        // Some netupdate stuff (multiplayer compatibility).
        if (collision)
        {
            if (npc.localAI[0] != 1)
                npc.netUpdate = true;

            npc.localAI[0] = 1f;
        }
        else
        {
            if (npc.localAI[0] != 0)
                npc.netUpdate = true;

            npc.localAI[0] = 0f;
        }

        // Force a netupdate if the NPC's velocity changed sign and it was not "just hit" by a player
        if (((npc.velocity.X > 0 && npc.oldVelocity.X < 0) || (npc.velocity.X < 0 && npc.oldVelocity.X > 0) ||
             (npc.velocity.Y > 0 && npc.oldVelocity.Y < 0) || (npc.velocity.Y < 0 && npc.oldVelocity.Y > 0)) &&
            !npc.justHit)
            npc.netUpdate = true;
    }

    public static void BodyTailFollow(NPC npc, NPC followingNpc)
    {
        if (!npc.HasValidTarget)
            npc.TargetClosest(true);

        if (Main.player[npc.target].dead && npc.timeLeft > 30000)
            npc.timeLeft = 10;

        // Match taking damage
        npc.dontTakeDamage = Main.npc[npc.realLife].dontTakeDamage;

        NPC following = followingNpc;
        if (Main.netMode != NetmodeID.MultiplayerClient)
        {
            // Some of these conditions are possible if the body/tail segment was spawned individually
            // Kill the segment if the segment NPC it's following is no longer valid
            if (following is null || !following.active || following.friendly || following.townNPC ||
                following.lifeMax <= 5)
            {
                npc.life = 0;
                npc.HitEffect(0, 10);
                npc.active = false;
            }
        }

        if (following is not null)
        {
            // Follow behind the segment "in front" of this NPC
            // Use the current NPC.Center to calculate the direction towards the "parent NPC" of this NPC.
            float dirX = following.Center.X - npc.Center.X;
            float dirY = following.Center.Y - npc.Center.Y;
            // We then use Atan2 to get a correct rotation towards that parent NPC.
            // Assumes the sprite for the NPC points upward.  You might have to modify this line to properly account for your NPC's orientation
            npc.rotation = (float) Math.Atan2(dirY, dirX) + MathHelper.PiOver2;
            // We also get the length of the direction vector.
            float length = (float) Math.Sqrt(dirX * dirX + dirY * dirY);
            // We calculate a new, correct distance.
            float dist = (length - npc.width) / length;
            float posX = dirX * dist;
            float posY = dirY * dist;

            // Reset the velocity of this NPC, because we don't want it to move on its own
            npc.velocity = Vector2.Zero;
            // And set this NPCs position accordingly to that of this NPCs parent NPC.
            npc.position.X += posX;
            npc.position.Y += posY;
        }
    }
}