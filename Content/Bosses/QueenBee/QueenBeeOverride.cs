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

namespace AcidicBosses.Content.Bosses.QueenBee;

public class QueenBeeOverride : AcidicNPCOverride
{
	protected override int OverriddenNpc => NPCID.QueenBee;
    
	#region Phases  
  
	private enum PhaseState  
	{
		One
	}  
	  
	private PhaseState CurrentPhase  
	{  
	    get => (PhaseState) Npc.ai[1];  
	    set => Npc.ai[1] = (float) value;  
	}  
	  
	private Action CurrentAi => CurrentPhase switch  
	{
		PhaseState.One => Phase_One,
	    _ => throw new UsageException(  
	        $"The PhaseState {CurrentPhase} and does not have an ai")  
	};  
	  
	#endregion

	#region Attacks  
	
	private enum Attack  
	{
		DashFromLeft,
		DashFromRight,
		StingerShot
	}

	private Attack[] phaseOneAP =
	{
		Attack.DashFromLeft,
		Attack.DashFromRight,
		Attack.DashFromLeft,
		Attack.StingerShot,
		Attack.StingerShot,
		Attack.StingerShot,
	};
	  
	private Attack[] CurrentAttackPattern => CurrentPhase switch  
	{
		PhaseState.One => phaseOneAP,
	    _ => throw new UsageException(  
	        $"QB is in the PhaseState {CurrentPhase} and does not have an attack pattern")  
	};  
	  
	private int CurrentAttackIndex  
	{  
	    get => (int) Npc.ai[2];  
	    set => Npc.ai[2] = value;  
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

	private bool useDashSprite = false;
	  
	private int AiTimer  
	{  
	    get => (int) Npc.ai[0];  
	    set => Npc.ai[0] = value;  
	}  
	  
	public override void OnFirstFrame(NPC npc)  
	{
	    CurrentPhase = PhaseState.One;  
	    AiTimer = 0;
	}
	  
	public override bool AcidAI(NPC npc)  
	{
		if (AiTimer > 0 && !countUpTimer)  
	        AiTimer--;  
	  
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
	  
	    if (isFleeing) FleeAI();  
	    else CurrentAi.Invoke();  
	  
	    if (countUpTimer)  
	        AiTimer++;  
	  
	    return false;  
	}
	
	private void FleeAI()  
	{  
	    // Put Flee Behavior here
	}
	
	#region Phase AIs

	private void Phase_One()
	{
		if (AiTimer > 0 && !countUpTimer)  
		{  
			// Idle Behavior
			Attack_HoverAbove();
			return;  
		}  
  
		bool isDone = false;  
		switch (CurrentAttack)  
		{  
			
			case Attack.DashFromLeft:
				Attack_DashFromSide(out isDone, Attack_HoverLeft);
				if (isDone) AiTimer = 60;
				break;
			case Attack.DashFromRight:
				Attack_DashFromSide(out isDone, Attack_HoverRight);
				if (isDone) AiTimer = 60;
				break;
			case Attack.StingerShot:
				Attack_StingerShot();
				AiTimer = 30;
				break;
		}  
  
		if (isDone) NextAttack();
	}

	#endregion

	#region Attack Behaviors

	private static float flightSpeed = 15f;
	private static float flightAccel = 0.25f;

	private Vector2 StingerPos => Npc.Center + new Vector2(0, Npc.height / 2f);

	private void Attack_HoverAbove()
	{
		const float distance = 400f;
		
		Npc.spriteDirection = Npc.direction;
		var target = Main.player[Npc.target];
		var goal = target.Center;
		goal.Y -= distance;

		Npc.SimpleFlyMovement(Npc.DirectionTo(goal) * flightSpeed, flightAccel);
		SetDirection();
	}
	
	private void Attack_HoverLeft()
	{
		const float distance = 500f;

		Npc.spriteDirection = Npc.direction;
		var target = Main.player[Npc.target];
		var goal = target.Center;
		goal.X -= distance;

		Npc.SimpleFlyMovement(Npc.DirectionTo(goal) * flightSpeed, flightAccel);
		SetDirection();
	}
	
	private void Attack_HoverRight()
	{
		const float distance = 500f;
		
		Npc.spriteDirection = Npc.direction;
		var target = Main.player[Npc.target];
		var goal = target.Center;
		goal.X += distance;

		Npc.SimpleFlyMovement(Npc.DirectionTo(goal) * flightSpeed, flightAccel);
		SetDirection();
	}

	private void Attack_Dash()
	{
		const float speed = 15f;
		
		var direction = 0;
		var target = Main.player[Npc.target].Center;
		if (target.X > Npc.Center.X) direction = 1;
		else direction = -1;

		Npc.velocity = Vector2.UnitX * speed * direction;
	}

	private void Attack_DashFromSide(out bool isDone, Action hoverFunction)
	{
		const int dashLength = 60;
		const int indicateTime = 15;
		const int positionLength = 90;

		isDone = false;
		countUpTimer = true;

		if (AiTimer < positionLength - indicateTime)
		{
			hoverFunction();
		}

		else if (AiTimer <= positionLength)
		{
			useDashSprite = true;
			Npc.SimpleFlyMovement(Vector2.Zero, flightAccel * 2f);
		}

		if (AiTimer == positionLength)
		{
			Attack_Dash();
			SoundEngine.PlaySound(SoundID.Item32, Npc.Center);
		}

		if (AiTimer >= positionLength + dashLength)
		{
			isDone = true;
			countUpTimer = false;
			useDashSprite = false;
		}
	}

	private void Attack_StingerShot()
	{
		SoundEngine.PlaySound(SoundID.Item17, Npc.Center);

		if (Main.netMode != NetmodeID.MultiplayerClient)
		{
			var target = Main.player[Npc.target].Center;
			NewStinger(StingerPos, StingerPos.DirectionTo(target) * 10f);
		}
	}

	private Projectile NewStinger(Vector2 pos, Vector2 vel)
	{
		return Projectile.NewProjectileDirect(Npc.GetSource_FromAI(), pos, vel, ProjectileID.QueenBeeStinger,
			Npc.damage / 4, 3);
	}

	#endregion

	#endregion

	#region Drawing

	private void SetDirection()
	{
		Npc.direction = MathF.Sign(Npc.Center.X - Main.player[Npc.target].Center.X);
	}

	public override void FindFrame(NPC npc, int frameHeight)
	{
		if (npc.frameCounter > 4.0)
		{
			npc.frame.Y += frameHeight;
			npc.frameCounter = 0.0;
		}

		if (npc.frame.Y < frameHeight * 4)
		{
			npc.frame.Y = frameHeight * 4;
		}
		if (npc.frame.Y >= frameHeight * 12)
		{
			npc.frame.Y = frameHeight * 4;
		}
	}

	public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color lightColor)  
	{  
	    var drawPos = npc.Center - Main.screenPosition;  
	    var texture = TextureAssets.Npc[npc.type].Value;  
	    var origin = npc.frame.Size() * 0.5f;  
	    lightColor *= npc.Opacity;
	    
	    var effects = SpriteEffects.None;
	    if (npc.direction < 0) effects = SpriteEffects.FlipHorizontally;
	    
	    var frame = npc.frame;
	    if (useDashSprite) frame.Y %= 4 * npc.frame.Height;

	    spriteBatch.Draw(
	        texture, drawPos,  
	        frame, lightColor,  
	        npc.rotation, origin, npc.scale,  
	        effects, 0f);  
	        
	    return false;  
	}  

	#endregion

	public override void SendAcidAI(NPC npc, BitWriter bitWriter, BinaryWriter binaryWriter)
	{
		bitWriter.WriteBit(useDashSprite);
	}

	public override void ReceiveAcidAI(NPC npc, BitReader bitReader, BinaryReader binaryReader)
	{
		useDashSprite = bitReader.ReadBit();
	}
}