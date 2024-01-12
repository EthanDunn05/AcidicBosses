using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace AcidicBosses.Content.Bosses.EoW;

public class EoWOverride : AcidicNPCOverride
{
    // Apply all three types in this one override
    protected override int OverriddenNpc => 0;

    private bool IsHead => Npc.type == NPCID.EaterofWorldsHead;
    private bool IsBody => Npc.type == NPCID.EaterofWorldsBody;
    private bool IsTail => Npc.type == NPCID.EaterofWorldsTail;

    private NPC MainHead => Main.npc[Npc.realLife];

    public override void SetDefaults(NPC entity)
    {
        // entity.lifeMax = 10080;
    }

    public override bool AppliesToEntity(NPC entity, bool lateInstantiation)
    {
        return entity.type is NPCID.EaterofWorldsHead or NPCID.EaterofWorldsBody or NPCID.EaterofWorldsTail;
    }

    public override bool? DrawHealthBar(NPC npc, byte hbPosition, ref float scale, ref Vector2 position)
    {
        if (IsHead) return null;
        return false;
    }

    #region Phases

    private enum PhaseState
    {
        Intro
    }

    private PhaseState CurrentPhase
    {
        get => (PhaseState) ExtraAI[1];
        set => ExtraAI[1] = (float) value;
    }

    private Action CurrentAi => CurrentPhase switch
    {
        PhaseState.Intro => Phase_Intro,
        _ => throw new UsageException(
            $"The PhaseState {CurrentPhase} and does not have an ai")
    };

    #endregion

    #region Attacks

    private enum Attack
    {
    }

    private Attack[] CurrentAttackPattern => CurrentPhase switch
    {
        _ => throw new UsageException(
            $"Boss is in the PhaseState {CurrentPhase} and does not have an attack pattern")
    };

    private int CurrentAttackIndex
    {
        get => (int) ExtraAI[0];
        set => ExtraAI[0] = value;
    }

    private Attack CurrentAttack => CurrentAttackPattern[CurrentAttackIndex];

    private void NextAttack()
    {
        CurrentAttackIndex = (CurrentAttackIndex + 1) % CurrentAttackPattern.Length;
    }

    #endregion

    #region AI

    private bool countUpTimer = false;

    private bool isFleeing = false;

    // Worm parts act as a doubly linked list ordered from head to tail
    private int nextPart
    {
        get => (int) Npc.ai[0];
        set => Npc.ai[0] = value;
    }

    private NPC NextNpc => Main.npc[nextPart];

    private int prevPart
    {
        get => (int) Npc.ai[1];
        set => Npc.ai[1] = value;
    }

    private NPC PrevNpc => Main.npc[prevPart];

    private int lengthFollowing
    {
        get => (int) Npc.ai[2];
        set => Npc.ai[2] = value;
    }

    private int totalParts = 75; // Slightly more than expert mode

    private int AiTimer
    {
        get => (int) Npc.ai[3];
        set => Npc.ai[3] = value;
    }

    public override void OnFirstFrame(NPC npc)
    {
        AiTimer = 0;
        CurrentPhase = PhaseState.Intro;

        if (IsHead)
        {
            Npc.realLife = Npc.whoAmI;
        }
    }

    public override bool AcidAI(NPC npc)
    {
        if (AiTimer > 0 && !countUpTimer) AiTimer--;

        npc.Opacity = MathHelper.Clamp(npc.Opacity + 0.08f, 0f, 1f);

        ManagePartList();

        if (Npc.whoAmI != Npc.realLife) SyncAI();

        // Spawn dust when this segment is underground for visibility
        if (CheckCollisionForDustSpawns())
        {
            if (Main.rand.NextBool(10))
                Dust.NewDust(Npc.position, Npc.width, Npc.height, DustID.Shadowflame);
        }

        // Check flee conditions
        var target = Main.player[npc.target];
        if (IsTargetGone(npc) && !isFleeing)
        {
            npc.TargetClosest();
            target = Main.player[npc.target];
            if (IsTargetGone(npc))
            {
                countUpTimer = true;
                isFleeing = true;
                AiTimer = 0;
            }
        }

        if (IsHead) HeadAI();
        if (IsBody) BodyAI();
        if (IsTail) TailAI();

        CurrentAi();

        if (countUpTimer)
            AiTimer++;

        return false;
    }

    private void HeadAI()
    {
        var collision = CheckCollisionForDustSpawns();
        HeadAI_CheckTargetDistance(ref collision);
        HeadAI_Movement(collision);
    }

    private void BodyAI()
    {
        BodyTailAI_Movement();
    }

    private void TailAI()
    {
        BodyTailAI_Movement();
    }

    private void SyncAI()
    {
        AiTimer = (int) MainHead.ai[3];

        var headOverride = MainHead.GetGlobalNPC<EoWOverride>();
        CurrentPhase = headOverride.CurrentPhase;
        CurrentAttackIndex = headOverride.CurrentAttackIndex;
    }

    private void FleeAI()
    {
        if (Main.netMode != NetmodeID.MultiplayerClient && (double) (Npc.position.Y / 16f) >
            (Main.rockLayer + (double) Main.maxTilesY) / 2.0)
        {
            Npc.active = false;
            int num63 = (int) Npc.ai[0];
            while (num63 > 0 && num63 < 200 && Main.npc[num63].active &&
                   Main.npc[num63].aiStyle == Npc.aiStyle)
            {
                int num73 = (int) Main.npc[num63].ai[0];
                Main.npc[num63].active = false;
                Npc.life = 0;
                if (Main.netMode == NetmodeID.Server)
                {
                    NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, num63);
                }

                num63 = num73;
            }

            if (Main.netMode == 2)
            {
                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, Npc.whoAmI);
            }
        }
    }

    #region Part Management

    private bool TryGetNextPart(out EoWOverride part)
    {
        var res = Main.npc[nextPart].TryGetGlobalNPC<EoWOverride>(out part);
        if (!res) Main.chatMonitor.NewText($"Could not get next {nextPart}");
        return res;
    }

    private bool TryGetPrevPart(out EoWOverride part)
    {
        var res = Main.npc[prevPart].TryGetGlobalNPC(out part);
        if (!res) Main.chatMonitor.NewText("Could not get prev");
        return res;
    }

    private void ManagePartList()
    {
        if (Main.netMode == NetmodeID.MultiplayerClient) return;

        // If there's no next part
        if ((IsHead || IsBody) && nextPart == 0)
        {
            // Spawn the next body part
            if (IsHead)
            {
                lengthFollowing = totalParts;
                nextPart = NewBody(Npc.position).whoAmI;
            }

            // If there's more body to spawn
            else if (IsBody && lengthFollowing > 0)
            {
                nextPart = NewBody(Npc.position).whoAmI;
            }

            // Spawn tail if there's no more body to spawn
            else
            {
                nextPart = NewTail(Npc.position).whoAmI;
            }

            // Sync previous part and length for next part
            NextNpc.ai[1] = Npc.whoAmI;
            NextNpc.ai[2] = lengthFollowing - 1;

            // Sync HP to head
            NextNpc.realLife = Npc.realLife;
            NextNpc.life = Npc.life;
            NextNpc.lifeMax = Npc.lifeMax;

            Npc.netUpdate = true;
        }

        // Don't allow for dangling parts
        if (!NextNpc.active && !PrevNpc.active) DestroyPart(Npc);
        if (IsHead && !NextNpc.active) DestroyPart(Npc);
        if (IsTail && !PrevNpc.active) DestroyPart(Npc);

        // Transform
        if (IsBody && (!PrevNpc.active || PrevNpc.aiStyle != Npc.aiStyle)) TransformHead();
        if (IsBody && (!NextNpc.active || NextNpc.aiStyle != Npc.aiStyle)) TransformTail();
    }

    private void TransformHead()
    {
        Npc.type = NPCID.EaterofWorldsHead;
        int realLife = Npc.realLife;
        int preLife = Npc.life;
        int aiTimerHold = AiTimer;
        int whoAmI = Npc.whoAmI;
        int next = nextPart;

        // Actually transform the body segment into a head segment.
        Npc.SetDefaultsKeepPlayerInteraction(Npc.type);
        Npc.realLife = realLife;
        Npc.life = preLife;
        Npc.whoAmI = whoAmI;
        AiTimer = aiTimerHold;
        nextPart = next;
        SyncAI();
        Npc.TargetClosest();
        Npc.netUpdate = true;
        Npc.netSpam = 0;
    }

    private void TransformTail()
    {
        Npc.type = NPCID.EaterofWorldsTail;
        int realLife = Npc.realLife;
        int preLife = Npc.life;
        int whoAmI = Npc.whoAmI;
        int prev = prevPart;

        // Actually transform the body segment into a head segment.
        Npc.SetDefaultsKeepPlayerInteraction(Npc.type);
        Npc.realLife = realLife;
        Npc.life = preLife;
        Npc.whoAmI = whoAmI;
        prevPart = prev;
        Npc.TargetClosest();
        Npc.netUpdate = true;
        Npc.netSpam = 0;
    }

    private void DestroyPart(NPC npc)
    {
        npc.life = 0;
        npc.HitEffect();
        npc.checkDead();
    }

    #endregion

    #region Phase AIs

    private void Phase_Intro()
    {
        countUpTimer = true;

        if (AiTimer == 300)
        {
            Split();
        }
    }

    #endregion

    #region Attack Behaviors

    #region Movement

    // Movement taken from Worm.cs in ExampleMod

    private int MaxDistanceForUsingTileCollision => 1000;

    private Vector2? ForcedTargetPosition { get; set; }

    public float MoveSpeed => 8f;

    public float Acceleration => 0.07f;

    private bool CheckCollisionForDustSpawns()
    {
        int minTilePosX = (int) (Npc.Left.X / 16) - 1;
        int maxTilePosX = (int) (Npc.Right.X / 16) + 2;
        int minTilePosY = (int) (Npc.Top.Y / 16) - 1;
        int maxTilePosY = (int) (Npc.Bottom.Y / 16) + 2;

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

                    if (Npc.Right.X > tileWorld.X && Npc.Left.X < tileWorld.X + 16 && Npc.Bottom.Y > tileWorld.Y &&
                        Npc.Top.Y < tileWorld.Y + 16)
                    {
                        // Collision found
                        collision = true;
                    }
                }
            }
        }

        return collision;
    }

    private void HeadAI_CheckTargetDistance(ref bool collision)
    {
        // If there is no collision with tiles, we check if the distance between this NPC and its target is too large, so that we can still trigger "collision".
        if (!collision)
        {
            Rectangle hitbox = Npc.Hitbox;

            int maxDistance = MaxDistanceForUsingTileCollision;

            bool tooFar = true;

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Rectangle areaCheck;

                Player player = Main.player[i];

                if (ForcedTargetPosition is Vector2 target)
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

    private void HeadAI_Movement(bool collision)
    {
        // MoveSpeed determines the max speed at which this NPC can move.
        // Higher value = faster speed.
        float speed = MoveSpeed;
        // acceleration is exactly what it sounds like. The speed at which this NPC accelerates.
        float acceleration = Acceleration;

        float targetXPos, targetYPos;

        Player playerTarget = Main.player[Npc.target];

        Vector2 forcedTarget = ForcedTargetPosition ?? playerTarget.Center;
        // Using a ValueTuple like this allows for easy assignment of multiple values
        (targetXPos, targetYPos) = (forcedTarget.X, forcedTarget.Y);

        // Copy the value, since it will be clobbered later
        Vector2 npcCenter = Npc.Center;

        float targetRoundedPosX = (float) ((int) (targetXPos / 16f) * 16);
        float targetRoundedPosY = (float) ((int) (targetYPos / 16f) * 16);
        npcCenter.X = (float) ((int) (npcCenter.X / 16f) * 16);
        npcCenter.Y = (float) ((int) (npcCenter.Y / 16f) * 16);
        float dirX = targetRoundedPosX - npcCenter.X;
        float dirY = targetRoundedPosY - npcCenter.Y;

        float length = (float) Math.Sqrt(dirX * dirX + dirY * dirY);

        // If we do not have any type of collision, we want the NPC to fall down and de-accelerate along the X axis.
        if (!collision)
            HeadAI_Movement_HandleFallingFromNoCollision(dirX, speed, acceleration);
        else
        {
            // Else we want to play some audio (soundDelay) and move towards our target.
            HeadAI_Movement_PlayDigSounds(length);

            HeadAI_Movement_HandleMovement(dirX, dirY, length, speed, acceleration);
        }

        HeadAI_Movement_SetRotation(collision);
    }

    private void HeadAI_Movement_HandleFallingFromNoCollision(float dirX, float speed, float acceleration)
    {
        // Keep searching for a new target
        Npc.TargetClosest(true);

        // Constant gravity of 0.11 pixels/tick
        Npc.velocity.Y += 0.11f;

        // Ensure that the Npc does not fall too quickly
        if (Npc.velocity.Y > speed)
            Npc.velocity.Y = speed;

        // The following behavior mimics vanilla worm movement
        if (Math.Abs(Npc.velocity.X) + Math.Abs(Npc.velocity.Y) < speed * 0.4f)
        {
            // Velocity is sufficiently fast, but not too fast
            if (Npc.velocity.X < 0.0f)
                Npc.velocity.X -= acceleration * 1.1f;
            else
                Npc.velocity.X += acceleration * 1.1f;
        }
        else if (Npc.velocity.Y == speed)
        {
            // Npc has reached terminal velocity
            if (Npc.velocity.X < dirX)
                Npc.velocity.X += acceleration;
            else if (Npc.velocity.X > dirX)
                Npc.velocity.X -= acceleration;
        }
        else if (Npc.velocity.Y > 4)
        {
            if (Npc.velocity.X < 0)
                Npc.velocity.X += acceleration * 0.9f;
            else
                Npc.velocity.X -= acceleration * 0.9f;
        }
    }

    private void HeadAI_Movement_PlayDigSounds(float length)
    {
        if (Npc.soundDelay == 0)
        {
            // Play sounds quicker the closer the Npc is to the target location
            float num1 = length / 40f;

            if (num1 < 10)
                num1 = 10f;

            if (num1 > 20)
                num1 = 20f;

            Npc.soundDelay = (int) num1;

            SoundEngine.PlaySound(SoundID.WormDig, Npc.position);
        }
    }

    private void HeadAI_Movement_HandleMovement(float dirX, float dirY, float length, float speed, float acceleration)
    {
        float absDirX = Math.Abs(dirX);
        float absDirY = Math.Abs(dirY);
        float newSpeed = speed / length;
        dirX *= newSpeed;
        dirY *= newSpeed;

        if ((Npc.velocity.X > 0 && dirX > 0) || (Npc.velocity.X < 0 && dirX < 0) || (Npc.velocity.Y > 0 && dirY > 0) ||
            (Npc.velocity.Y < 0 && dirY < 0))
        {
            // The Npc is moving towards the target location
            if (Npc.velocity.X < dirX)
                Npc.velocity.X += acceleration;
            else if (Npc.velocity.X > dirX)
                Npc.velocity.X -= acceleration;

            if (Npc.velocity.Y < dirY)
                Npc.velocity.Y += acceleration;
            else if (Npc.velocity.Y > dirY)
                Npc.velocity.Y -= acceleration;

            // The intended Y-velocity is small AND the Npc is moving to the left and the target is to the right of the Npc or vice versa
            if (Math.Abs(dirY) < speed * 0.2 && ((Npc.velocity.X > 0 && dirX < 0) || (Npc.velocity.X < 0 && dirX > 0)))
            {
                if (Npc.velocity.Y > 0)
                    Npc.velocity.Y += acceleration * 2f;
                else
                    Npc.velocity.Y -= acceleration * 2f;
            }

            // The intended X-velocity is small AND the Npc is moving up/down and the target is below/above the Npc
            if (Math.Abs(dirX) < speed * 0.2 && ((Npc.velocity.Y > 0 && dirY < 0) || (Npc.velocity.Y < 0 && dirY > 0)))
            {
                if (Npc.velocity.X > 0)
                    Npc.velocity.X = Npc.velocity.X + acceleration * 2f;
                else
                    Npc.velocity.X = Npc.velocity.X - acceleration * 2f;
            }
        }
        else if (absDirX > absDirY)
        {
            // The X distance is larger than the Y distance.  Force movement along the X-axis to be stronger
            if (Npc.velocity.X < dirX)
                Npc.velocity.X += acceleration * 1.1f;
            else if (Npc.velocity.X > dirX)
                Npc.velocity.X -= acceleration * 1.1f;

            if (Math.Abs(Npc.velocity.X) + Math.Abs(Npc.velocity.Y) < speed * 0.5)
            {
                if (Npc.velocity.Y > 0)
                    Npc.velocity.Y += acceleration;
                else
                    Npc.velocity.Y -= acceleration;
            }
        }
        else
        {
            // The X distance is larger than the Y distance.  Force movement along the X-axis to be stronger
            if (Npc.velocity.Y < dirY)
                Npc.velocity.Y += acceleration * 1.1f;
            else if (Npc.velocity.Y > dirY)
                Npc.velocity.Y -= acceleration * 1.1f;

            if (Math.Abs(Npc.velocity.X) + Math.Abs(Npc.velocity.Y) < speed * 0.5)
            {
                if (Npc.velocity.X > 0)
                    Npc.velocity.X += acceleration;
                else
                    Npc.velocity.X -= acceleration;
            }
        }
    }

    private void HeadAI_Movement_SetRotation(bool collision)
    {
        // Set the correct rotation for this Npc.
        // Assumes the sprite for the Npc points upward.  You might have to modify this line to properly account for your Npc's orientation
        Npc.rotation = Npc.velocity.ToRotation() + MathHelper.PiOver2;

        // Some netupdate stuff (multiplayer compatibility).
        if (collision)
        {
            if (Npc.localAI[0] != 1)
                Npc.netUpdate = true;

            Npc.localAI[0] = 1f;
        }
        else
        {
            if (Npc.localAI[0] != 0)
                Npc.netUpdate = true;

            Npc.localAI[0] = 0f;
        }

        // Force a netupdate if the Npc's velocity changed sign and it was not "just hit" by a player
        if (((Npc.velocity.X > 0 && Npc.oldVelocity.X < 0) || (Npc.velocity.X < 0 && Npc.oldVelocity.X > 0) ||
             (Npc.velocity.Y > 0 && Npc.oldVelocity.Y < 0) || (Npc.velocity.Y < 0 && Npc.oldVelocity.Y > 0)) &&
            !Npc.justHit)
            Npc.netUpdate = true;
    }

    private void BodyTailAI_Movement()
    {
        // Follow behind the segment "in front" of this NPC
        // Use the current NPC.Center to calculate the direction towards the "parent NPC" of this NPC.
        float dirX = PrevNpc.Center.X - Npc.Center.X;
        float dirY = PrevNpc.Center.Y - Npc.Center.Y;

        // We then use Atan2 to get a correct rotation towards that parent NPC.
        // Assumes the sprite for the NPC points upward.  You might have to modify this line to properly account for your NPC's orientation
        Npc.rotation = (float) Math.Atan2(dirY, dirX) + MathHelper.PiOver2;
        // We also get the length of the direction vector.
        float length = (float) Math.Sqrt(dirX * dirX + dirY * dirY);
        // We calculate a new, correct distance.
        float dist = (length - Npc.width) / length;
        float posX = dirX * dist;
        float posY = dirY * dist;

        // Reset the velocity of this NPC, because we don't want it to move on its own
        Npc.velocity = Vector2.Zero;
        // And set this NPCs position accordingly to that of this NPCs parent NPC.
        Npc.position.X += posX;
        Npc.position.Y += posY;
    }

    #endregion

    private void Split()
    {
        // Only have the head attempt this
        if (!IsHead) return;
        if (Main.netMode == NetmodeID.MultiplayerClient) return;

        // Count Connected Segments
        var segments = 1;
        var nextCheck = NextNpc;
        while (nextCheck.active && nextCheck.aiStyle == Npc.aiStyle)
        {
            segments++;
            nextCheck = Main.npc[(int) nextCheck.ai[0]];
        }

        // Destroy center segment
        var checkedSegments = 1;
        nextCheck = NextNpc;
        while (nextCheck.active && nextCheck.aiStyle == Npc.aiStyle)
        {
            checkedSegments++;
            nextCheck = Main.npc[(int) nextCheck.ai[0]];
            if (checkedSegments >= segments / 2) break;
        }

        // Destroy that center segment
        DestroyPart(nextCheck);
    }

    private NPC NewBody(Vector2 position)
    {
        return NPC.NewNPCDirect(Npc.GetSource_FromAI(), position, NPCID.EaterofWorldsBody, Npc.whoAmI);
    }

    private NPC NewTail(Vector2 position)
    {
        return NPC.NewNPCDirect(Npc.GetSource_FromAI(), position, NPCID.EaterofWorldsTail, Npc.whoAmI);
    }

    #endregion

    #endregion

    #region Drawing

    public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color lightColor)
    {
        var drawPos = npc.Center - Main.screenPosition;
        var texture = TextureAssets.Npc[npc.type].Value;
        var origin = npc.frame.Size() * 0.5f;
        lightColor *= npc.Opacity;

        spriteBatch.Draw(
            texture, drawPos,
            npc.frame, lightColor,
            npc.rotation, origin, npc.scale,
            SpriteEffects.None, 0f);

        return false;
    }

    #endregion

    public override void SendAcidAI(NPC npc, BitWriter bitWriter, BinaryWriter binaryWriter)
    {
    }

    public override void ReceiveAcidAI(NPC npc, BitReader bitReader, BinaryReader binaryReader)
    {
    }
}