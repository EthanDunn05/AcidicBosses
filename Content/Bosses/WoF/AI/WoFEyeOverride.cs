using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace AcidicBosses.Content.Bosses.WoF.AI;

public class WoFEyeOverride : AcidicNPCOverride
{
    protected override int OverriddenNpc => NPCID.WallofFleshEye;

    private NPC WoF => Main.npc[Main.wofNPCIndex];
    
    private WoFPartPosition EyePos
    {
        get => (WoFPartPosition) Npc.ai[0];
        set => Npc.ai[0] = (float) value;
    }
    
    private float WallDistance => WoF.ai[3];

    #region AI

    private bool countUpTimer = false;

    private int AiTimer
    {
        get => (int) Npc.ai[1];
        set => Npc.ai[1] = value;
    }

    public override void OnFirstFrame(NPC npc)
    {
        AiTimer = 0;
        Npc.realLife = Main.wofNPCIndex;
        Npc.life = WoF.life;
        Npc.lifeMax = WoF.lifeMax;
    }

    public override bool AcidAI(NPC npc)
    {
        if (AiTimer > 0 && !countUpTimer)
            AiTimer--;
        
        if (Main.wofNPCIndex < 0)
        {
            Npc.active = false;
            return false;
        }

        Npc.realLife = Main.wofNPCIndex;
        if (WoF.life > 0)
        {
            Npc.life = WoF.life;
        }
        
        // Sync stuff
        if ((EyePos & WoFPartPosition.Left) != 0)
        {
            Npc.position.X = WoF.position.X - WallDistance;
            Npc.direction = 1;
            Npc.spriteDirection = Npc.direction;
            MoveToWallPosLeft();
        }
        else
        {
            Npc.position.X = WoF.position.X + WallDistance;
            Npc.direction = -1;
            Npc.spriteDirection = Npc.direction;
            MoveToWallPosRight();
        }
        
        Npc.TargetClosest_WOF();
 
        if (countUpTimer)
            AiTimer++;

        return false;
    }

    

    #region Phase AIs

    #endregion

    #region Attack Behaviors

    // Put attack methods here

    #endregion

    #endregion
    
    #region Drawing

    private void MoveToWallPosRight()
    {
        var posY = (WoFSystem.WofDrawAreaBottomRight + WoFSystem.WofDrawAreaTopRight) / 2f;
        if ((EyePos & WoFPartPosition.Top) != 0)
        {
            posY = (posY + WoFSystem.WofDrawAreaBottomRight) / 2f;
        }
        else
        {
            posY = (posY + WoFSystem.WofDrawAreaTopRight) / 2f;
        }
        posY -= Npc.height / 2f;
        
        MoveToWallPos(posY);
    }
    
    private void MoveToWallPosLeft()
    {
        var posY = (WoFSystem.WofDrawAreaBottomLeft + WoFSystem.WofDrawAreaTopLeft) / 2f;
        if ((EyePos & WoFPartPosition.Top) != 0)
        {
            posY = (posY + WoFSystem.WofDrawAreaBottomLeft) / 2f;
        }
        else
        {
            posY = (posY + WoFSystem.WofDrawAreaTopLeft) / 2f;
        }
        posY -= Npc.height / 2f;
        
        MoveToWallPos(posY);
    }

    private void MoveToWallPos(float posY)
    {
        if (Npc.position.Y > posY + 1f)
        {
            Npc.velocity.Y = -1f;
        }
        else if (Npc.position.Y < posY - 1f)
        {
            Npc.velocity.Y = 1f;
        }
        else
        {
            Npc.velocity.Y = 0f;
            Npc.position.Y = posY;
        }
        
        if (Npc.velocity.Y > 5f)
        {
            Npc.velocity.Y = 5f;
        }

        if (Npc.velocity.Y < -5f)
        {
            Npc.velocity.Y = -5f;
        }
    }

    #endregion
    
    protected override void LookTowards(Vector2 target, float power)
    {
        var offset = 0f;
        if (Npc.direction < 0) offset = MathHelper.Pi;
        Npc.rotation = Npc.rotation.AngleLerp(Npc.AngleTo(target) + offset, power);
    }
}