using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
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

    public override bool AppliesToEntity(NPC entity, bool lateInstantiation)
    {
        return entity.type is NPCID.EaterofWorldsHead or NPCID.EaterofWorldsBody or NPCID.EaterofWorldsTail;
    }

    #region Phases

    private enum PhaseState
    {
    }

    private PhaseState CurrentPhase
    {
        get => (PhaseState) ExtraAI[1];
        set => ExtraAI[1] = (float) value;
    }

    private Action CurrentAi => CurrentPhase switch
    {
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
    }

    public override bool AcidAI(NPC npc)
    {
        if (AiTimer > 0 && !countUpTimer) AiTimer--;

        npc.Opacity = MathHelper.Clamp(npc.Opacity + 0.08f, 0f, 1f);

        ManagePartList();

        // Flee when no players are alive or it is day  
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

        Move(Main.player[Npc.target].Center);

        if (countUpTimer)
            AiTimer++;

        return false;
    }

    private void FleeAI()
    {
        if (Main.netMode != 1 && (double) (Npc.position.Y / 16f) >
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

    #region Parts

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

            Npc.netUpdate = true;
        }

        // Don't allow for dangling parts
        if (!NextNpc.active && !PrevNpc.active) DestroyPart();
        if (IsHead && !NextNpc.active) DestroyPart();
        if (IsTail && !PrevNpc.active) DestroyPart();

        // Transform
        if (IsBody && (!PrevNpc.active || PrevNpc.aiStyle != Npc.aiStyle)) TransformHead();
        if (IsBody && (!NextNpc.active || NextNpc.aiStyle != Npc.aiStyle)) TransformTail();
    }

    private void TransformHead()
    {
        Npc.type = NPCID.EaterofWorldsHead;
        float segmentLifeRatio = Npc.life / (float) Npc.lifeMax;
        int whoAmI = Npc.whoAmI;
        int next = nextPart;

        // Actually transform the body segment into a head segment.
        Npc.SetDefaultsKeepPlayerInteraction(Npc.type);
        Npc.life = (int) (Npc.lifeMax * segmentLifeRatio);
        Npc.whoAmI = whoAmI;
        nextPart = next;
        Npc.TargetClosest();
        Npc.netUpdate = true;
        Npc.netSpam = 0;
    }

    private void TransformTail()
    {
        Npc.type = NPCID.EaterofWorldsTail;
        float segmentLifeRatio = Npc.life / (float) Npc.lifeMax;
        int whoAmI = Npc.whoAmI;
        int prev = prevPart;

        // Actually transform the body segment into a head segment.
        Npc.SetDefaultsKeepPlayerInteraction(Npc.type);
        Npc.life = (int) (Npc.lifeMax * segmentLifeRatio);
        Npc.whoAmI = whoAmI;
        prevPart = prev;
        Npc.TargetClosest();
        Npc.netUpdate = true;
        Npc.netSpam = 0;
    }

    private void DestroyPart()
    {
        Npc.life = 0;
        Npc.HitEffect();
        Npc.checkDead();
    }

    private void HeadAI()
    {
    }

    private void BodyAI()
    {
    }

    private void TailAI()
    {
    }

    #endregion

    #region Phase AIs

    #endregion

    #region Attack Behaviors

    private bool GetCanFly()
    {
        // Adapted from vanilla code
        var tilePositionX = (int) (Npc.position.X / 16f) - 1;
        var tileWidthPosX = (int) ((Npc.position.X + Npc.width) / 16f) + 2;
        var tilePositionY = (int) (Npc.position.Y / 16f) - 1;
        var tileWidthPosY = (int) ((Npc.position.Y + Npc.height) / 16f) + 2;
        if (tilePositionX < 0)
            tilePositionX = 0;
        if (tileWidthPosX > Main.maxTilesX)
            tileWidthPosX = Main.maxTilesX;
        if (tilePositionY < 0)
            tilePositionY = 0;
        if (tileWidthPosY > Main.maxTilesY)
            tileWidthPosY = Main.maxTilesY;

        // Fly or not
        bool inTiles = false;
        if (!inTiles)
        {
            for (int i = tilePositionX; i < tileWidthPosX; i++)
            {
                for (int j = tilePositionY; j < tileWidthPosY; j++)
                {
                    if (Main.tile[i, j] != null &&
                        ((Main.tile[i, j].HasUnactuatedTile && (Main.tileSolid[Main.tile[i, j].TileType] ||
                                                                (Main.tileSolidTop[Main.tile[i, j].TileType] &&
                                                                 Main.tile[i, j].TileFrameY == 0))) ||
                         Main.tile[i, j].LiquidAmount > 64))
                    {
                        Vector2 vector;
                        vector.X = i * 16;
                        vector.Y = j * 16;
                        if (Npc.position.X + Npc.width > vector.X && Npc.position.X < vector.X + 16f &&
                            Npc.position.Y + Npc.height > vector.Y && Npc.position.Y < vector.Y + 16f)
                        {
                            inTiles = true;
                            if (Main.rand.NextBool(100) && Main.tile[i, j].HasUnactuatedTile)
                            {
                                WorldGen.KillTile(i, j, true, true, false);
                            }
                        }
                    }
                }
            }
        }

        if (!inTiles && IsHead)
        {
            Rectangle rectangle = new Rectangle((int) Npc.position.X, (int) Npc.position.Y, Npc.width, Npc.height);
            int num37 = 1000;
            bool flag3 = true;
            for (int num38 = 0; num38 < 255; num38++)
            {
                if (Main.player[num38].active)
                {
                    Rectangle rectangle2 = new Rectangle((int) Main.player[num38].position.X - num37,
                        (int) Main.player[num38].position.Y - num37, num37 * 2, num37 * 2);
                    if (rectangle.Intersects(rectangle2))
                    {
                        flag3 = false;
                        break;
                    }
                }
            }

            if (flag3)
            {
                inTiles = true;
            }
        }

        return inTiles;
    }

    private void Move(Vector2 target)
    {
        // Movement taken from vanilla

        float maxSpeed = 8f;
        float acceleration = 0.07f;

        Vector2 segDirection = new Vector2(Npc.position.X + (float) Npc.width * 0.5f, Npc.position.Y + (float) Npc.height * 0.5f);
        float targetX = target.X;
        float targetY = target.Y;

        targetX = (int) (targetX / 16f) * 16;
        targetY = (int) (targetY / 16f) * 16;
        segDirection.X = (int) (segDirection.X / 16f) * 16;
        segDirection.Y = (int) (segDirection.Y / 16f) * 16;
        targetX -= segDirection.X;
        targetY -= segDirection.Y;

        float targetDist = (float) Math.Sqrt(targetX * targetX + targetY * targetY);
        if (prevPart != 0)
        {
            try
            {
                segDirection = new Vector2(Npc.position.X + (float) Npc.width * 0.5f,
                    Npc.position.Y + (float) Npc.height * 0.5f);
                targetX = PrevNpc.position.X + (float) (PrevNpc.width / 2) -
                        segDirection.X;
                targetY = PrevNpc.position.Y + (float) (PrevNpc.height / 2) -
                        segDirection.Y;
            }
            catch
            {
            }

            Npc.rotation = (float) Math.Atan2(targetY, targetX) + 1.57f;
            targetDist = (float) Math.Sqrt(targetX * targetX + targetY * targetY);
            int width = Npc.width;
            width = (int) ((float) width * Npc.scale);
            targetDist = (targetDist - (float) width) / targetDist;
            targetX *= targetDist;
            targetY *= targetDist;
            Npc.velocity = Vector2.Zero;
            Npc.position.X += targetX;
            Npc.position.Y += targetY;
        }
        else
        {
            if (!GetCanFly())
            {
                Npc.TargetClosest();
                Npc.velocity.Y += 0.11f;
                if (Npc.velocity.Y > maxSpeed)
                {
                    Npc.velocity.Y = maxSpeed;
                }

                if ((double) (Math.Abs(Npc.velocity.X) + Math.Abs(Npc.velocity.Y)) < (double) maxSpeed * 0.4)
                {
                    if (Npc.velocity.X < 0f)
                    {
                        Npc.velocity.X -= acceleration * 1.1f;
                    }
                    else
                    {
                        Npc.velocity.X += acceleration * 1.1f;
                    }
                }
                else if (Npc.velocity.Y == maxSpeed)
                {
                    if (Npc.velocity.X < targetX)
                    {
                        Npc.velocity.X += acceleration;
                    }
                    else if (Npc.velocity.X > targetX)
                    {
                        Npc.velocity.X -= acceleration;
                    }
                }
                else if (Npc.velocity.Y > 4f)
                {
                    if (Npc.velocity.X < 0f)
                    {
                        Npc.velocity.X += acceleration * 0.9f;
                    }
                    else
                    {
                        Npc.velocity.X -= acceleration * 0.9f;
                    }
                }
            }
            else
            {
                if (Npc.soundDelay == 0)
                {
                    float num58 = targetDist / 40f;
                    if (num58 < 10f)
                    {
                        num58 = 10f;
                    }

                    if (num58 > 20f)
                    {
                        num58 = 20f;
                    }

                    Npc.soundDelay = (int) num58;
                    SoundEngine.PlaySound(SoundID.WormDig, Npc.position);
                }

                targetDist = (float) Math.Sqrt(targetX * targetX + targetY * targetY);
                float num59 = Math.Abs(targetX);
                float num60 = Math.Abs(targetY);
                float num61 = maxSpeed / targetDist;
                targetX *= num61;
                targetY *= num61;

                if ((Npc.velocity.X > 0f && targetX > 0f) || (Npc.velocity.X < 0f && targetX < 0f) ||
                    (Npc.velocity.Y > 0f && targetY > 0f) || (Npc.velocity.Y < 0f && targetY < 0f))
                {
                    if (Npc.velocity.X < targetX)
                    {
                        Npc.velocity.X += acceleration;
                    }
                    else if (Npc.velocity.X > targetX)
                    {
                        Npc.velocity.X -= acceleration;
                    }

                    if (Npc.velocity.Y < targetY)
                    {
                        Npc.velocity.Y += acceleration;
                    }
                    else if (Npc.velocity.Y > targetY)
                    {
                        Npc.velocity.Y -= acceleration;
                    }

                    if ((double) Math.Abs(targetY) < (double) maxSpeed * 0.2 &&
                        ((Npc.velocity.X > 0f && targetX < 0f) ||
                         (Npc.velocity.X < 0f && targetX > 0f)))
                    {
                        if (Npc.velocity.Y > 0f)
                        {
                            Npc.velocity.Y += acceleration * 2f;
                        }
                        else
                        {
                            Npc.velocity.Y -= acceleration * 2f;
                        }
                    }

                    if ((double) Math.Abs(targetX) < (double) maxSpeed * 0.2 &&
                        ((Npc.velocity.Y > 0f && targetY < 0f) ||
                         (Npc.velocity.Y < 0f && targetY > 0f)))
                    {
                        if (Npc.velocity.X > 0f)
                        {
                            Npc.velocity.X += acceleration * 2f;
                        }
                        else
                        {
                            Npc.velocity.X -= acceleration * 2f;
                        }
                    }
                }
                else if (num59 > num60)
                {
                    if (Npc.velocity.X < targetX)
                    {
                        Npc.velocity.X += acceleration * 1.1f;
                    }
                    else if (Npc.velocity.X > targetX)
                    {
                        Npc.velocity.X -= acceleration * 1.1f;
                    }

                    if ((double) (Math.Abs(Npc.velocity.X) + Math.Abs(Npc.velocity.Y)) < (double) maxSpeed * 0.5)
                    {
                        if (Npc.velocity.Y > 0f)
                        {
                            Npc.velocity.Y += acceleration;
                        }
                        else
                        {
                            Npc.velocity.Y -= acceleration;
                        }
                    }
                }
                else
                {
                    if (Npc.velocity.Y < targetY)
                    {
                        Npc.velocity.Y += acceleration * 1.1f;
                    }
                    else if (Npc.velocity.Y > targetY)
                    {
                        Npc.velocity.Y -= acceleration * 1.1f;
                    }

                    if ((double) (Math.Abs(Npc.velocity.X) + Math.Abs(Npc.velocity.Y)) < (double) maxSpeed * 0.5)
                    {
                        if (Npc.velocity.X > 0f)
                        {
                            Npc.velocity.X += acceleration;
                        }
                        else
                        {
                            Npc.velocity.X -= acceleration;
                        }
                    }
                }
            }

            Npc.rotation = (float) Math.Atan2(Npc.velocity.Y, Npc.velocity.X) + 1.57f;
            if (IsHead)
            {
                if (GetCanFly())
                {
                    if (Npc.localAI[0] != 1f)
                    {
                        Npc.netUpdate = true;
                    }

                    Npc.localAI[0] = 1f;
                }
                else
                {
                    if (Npc.localAI[0] != 0f)
                    {
                        Npc.netUpdate = true;
                    }

                    Npc.localAI[0] = 0f;
                }

                if (((Npc.velocity.X > 0f && Npc.oldVelocity.X < 0f) ||
                     (Npc.velocity.X < 0f && Npc.oldVelocity.X > 0f) ||
                     (Npc.velocity.Y > 0f && Npc.oldVelocity.Y < 0f) ||
                     (Npc.velocity.Y < 0f && Npc.oldVelocity.Y > 0f)) && !Npc.justHit)
                {
                    Npc.netUpdate = true;
                }
            }
        }
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

    #endregion

    public override void SendAcidAI(NPC npc, BitWriter bitWriter, BinaryWriter binaryWriter)
    {
    }

    public override void ReceiveAcidAI(NPC npc, BitReader bitReader, BinaryReader binaryReader)
    {
    }
}