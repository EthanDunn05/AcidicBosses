using System;
using AcidicBosses.Common.Effects;
using AcidicBosses.Common.RenderManagers;
using AcidicBosses.Common.Textures;
using AcidicBosses.Helpers;
using AcidicBosses.Helpers.ProjectileHelpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace AcidicBosses.Content.ProjectileBases;

public abstract class BaseBetsyFlame : ModProjectile, IAnchoredProjectile
{
    public override string Texture => TextureRegistry.TerrariaProjectile(ProjectileID.DD2BetsyFlameBreath);

    public float Offset => Projectile.ai[2];
    public int AnchorTo => (int) Projectile.ai[1];
    public bool AnchorPosition => true;
    public bool AnchorRotation => true;
    public bool RotateAroundCenter => true;
    public Vector2? StartOffset { get; set; }

    /// <summary>
    /// The color of the flame at the start. Betsy's is 255, 255, 255.
    /// The Alpha value is ignored.
    /// </summary>
    public abstract Color StartFlameColor { get; }
    
    /// <summary>
    /// The color of the flame at the end. Betsy's is 130, 30, 30.
    /// The Alpha value is ignored.
    /// </summary>
    public abstract Color EndFlameColor { get; }
    
    /// <summary>
    /// The dust to use as a flame. Usually a torch dust
    /// </summary>
    public abstract short FlameDust { get; }
    
    public override void SetStaticDefaults()
    {
        
    }
    
    public override void SetDefaults()
    {
        Projectile.width = 30;
        Projectile.height = 30;
        Projectile.tileCollide = false;
        Projectile.friendly = false;
        Projectile.hostile = true;
    }
    
    public static Projectile Create<T>(IEntitySource spawnSource, Vector2 position, float rotation, int damage, int knockback, int anchorTo) where T : BaseBetsyFlame
    {
        return ProjHelper.NewUnscaledProjectile(spawnSource, position, Vector2.Zero,
            ModContent.ProjectileType<T>(), damage, knockback, ai0: 0, ai1: anchorTo, ai2: rotation);
    }

    public override void OnHitPlayer(Player target, Player.HurtInfo info)
    {
	    target.AddBuff(BuffID.CursedInferno, 60 * Main.rand.Next(7, 11));
	    base.OnHitPlayer(target, info);
    }

    public override void AI()
    {
	    ref var aiTime = ref Projectile.ai[0];
	    var npc = Main.npc[(int)AnchorTo];
	    
	    // Anchor to npc
	    this.Anchor(Projectile);
	    
	    // Copied and modified from vanilla.
	    // I have tried to document this for my own understanding
		
		var progress1 = aiTime / 40f;
		if (progress1 > 1f)
		{
			progress1 = 1f;
		}
		var progress2 = (aiTime - 38f) / 40f;
		if (progress2 < 0f)
		{
			progress2 = 0f;
		}
		
		// This sets the lighting color
		DelegateMethods.v3_1 = EndFlameColor.ToVector3();
		
		// Light lines
		Utils.PlotTileLine(Projectile.Center + Projectile.rotation.ToRotationVector2() * 400f * progress2, Projectile.Center + Projectile.rotation.ToRotationVector2() * 400f * progress1, 16f, DelegateMethods.CastLight);
		Utils.PlotTileLine(Projectile.Center + Projectile.rotation.ToRotationVector2().RotatedBy(MathHelper.Pi / 16f) * 400f * progress2, Projectile.Center + Projectile.rotation.ToRotationVector2().RotatedBy(MathHelper.Pi / 16f) * 400f * progress1, 16f, DelegateMethods.CastLight);
		Utils.PlotTileLine(Projectile.Center + Projectile.rotation.ToRotationVector2().RotatedBy(-MathHelper.Pi / 16f) * 400f * progress2, Projectile.Center + Projectile.rotation.ToRotationVector2().RotatedBy(-MathHelper.Pi / 16f) * 400f * progress1, 16f, DelegateMethods.CastLight);
		
		// Flame dust
		if (progress2 == 0f && progress1 > 0.1f)
		{
			for (var i = 0; i < 3; i++)
			{
				var dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, FlameDust);
				dust.fadeIn = 1.5f;
				dust.velocity = Projectile.rotation.ToRotationVector2().RotatedBy(Main.rand.NextFloatDirection() * ((float)Math.PI / 12f)) * (0.5f + Main.rand.NextFloat() * 2.5f) * 15f;
				dust.velocity += npc.velocity * 2f;
				dust.noLight = true;
				dust.noGravity = true;
				dust.alpha = 200;
			}
		}
		
		// Smoke Gore
		if (Main.rand.NextBool(5) && aiTime >= 15f)
		{
			var gore = Gore.NewGoreDirect(Projectile.GetSource_FromAI(), Projectile.Center + Projectile.rotation.ToRotationVector2() * 300f - Utils.RandomVector2(Main.rand, -20f, 20f), Vector2.Zero, GoreID.Smoke1 + Main.rand.Next(3), 0.5f);
			gore.velocity *= 0.3f;
			gore.velocity += Projectile.rotation.ToRotationVector2() * 4f;
		}
		
		// Smoke Dust
		for (var j = 0; j < 1; j++)
		{
			var dust2 = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Smoke);
			dust2.fadeIn = 1.5f;
			dust2.scale = 0.4f;
			dust2.velocity = Projectile.rotation.ToRotationVector2().RotatedBy(Main.rand.NextFloatDirection() * ((float)Math.PI / 12f)) * (0.5f + Main.rand.NextFloat() * 2.5f) * 15f;
			dust2.velocity += npc.velocity * 2f;
			dust2.velocity *= 0.3f;
			dust2.noLight = true;
			dust2.noGravity = true;
			
			var rand = Main.rand.NextFloat();
			dust2.position = Vector2.Lerp(Projectile.Center + Projectile.rotation.ToRotationVector2() * 400f * progress2, Projectile.Center + Projectile.rotation.ToRotationVector2() * 400f * progress1, rand);
			dust2.position += Projectile.rotation.ToRotationVector2().RotatedBy(1.5707963705062866) * (20f + 100f * (rand - 0.5f));
		}
		
		// Progress animation
		Projectile.frameCounter++;
		
		// Progress timer
		aiTime += 1f;
		if (aiTime >= 78f)
		{
			Projectile.Kill();
		}
    }

    public override bool PreDraw(ref Color lightColor)
    {
	    DrawRenderTargetSystem.DrawToTarget(ModRenderTargets.ProjectileBloom, DrawFlame);
		
		return false;
    }

    private void DrawFlame(SpriteBatch spriteBatch)
    {
	    // Don't worry about how this works. I stole it from vanilla
	    // I can't decipher it either.
	    
        Vector2 center2 = Projectile.Center;
		center2 -= Main.screenPosition;
		float num204 = 40f;
		float num205 = num204 * 2f;
		float num206 = (float)Projectile.frameCounter / num204;
		Texture2D value197 = TextureAssets.Projectile[Projectile.type].Value;
		
		// Color gradient
		Color color1 = new Color(StartFlameColor.R, StartFlameColor.G, StartFlameColor.B, 0);
		Color color2 = new Color(EndFlameColor.R, EndFlameColor.G, EndFlameColor.B, 200);
		Color color3 = new Color(0, 0, 0, 30);
		ulong seed = 1uL;
		for (float i = 0f; i < 15f; i += 1f)
		{
			float num208 = Utils.RandomFloat(ref seed) * 0.25f - 0.125f;
			Vector2 value198 = (Projectile.rotation + num208).ToRotationVector2();
			Vector2 value199 = center2 + value198 * 400f;
			float num209 = num206 + i * (1f / 15f);
			int num210 = (int)(num209 / (1f / 15f));
			num209 %= 1f;
			if ((!(num209 > num206 % 1f) || !((float)Projectile.frameCounter < num204)) && (!(num209 < num206 % 1f) || !((float)Projectile.frameCounter >= num205 - num204)))
			{
				// I'm tempted to rewrite this awful bit of code
				// So many ternary operators and lerps in lerps
				var color = 
					(num209 < 0.1f) ? Color.Lerp(Color.Transparent, color1, Utils.GetLerpValue(0f, 0.1f, num209, clamped: true)) 
					: (num209 < 0.35f) ? color1 
					: (num209 < 0.7f) ? Color.Lerp(color1, color2, Utils.GetLerpValue(0.35f, 0.7f, num209, clamped: true)) 
					: (num209 < 0.9f) ? Color.Lerp(color2, color3, Utils.GetLerpValue(0.7f, 0.9f, num209, clamped: true)) 
					: (!(num209 < 1f)) ? Color.Transparent 
					: Color.Lerp(color3, Color.Transparent, Utils.GetLerpValue(0.9f, 1f, num209, clamped: true));
				
				float num211 = 0.9f + num209 * 0.8f;
				num211 *= num211;
				num211 *= 0.8f;
				Vector2 position29 = Vector2.SmoothStep(center2, value199, num209);
				Rectangle rectangle26 = value197.Frame(1, 7, 0, (int)(num209 * 7f));
				spriteBatch.Draw(value197, position29, rectangle26, color, Projectile.rotation + (float)Math.PI * 2f * (num209 + Main.GlobalTimeWrappedHourly * 1.2f) * 0.2f + (float)num210 * ((float)Math.PI * 2f / 5f), rectangle26.Size() / 2f, num211, SpriteEffects.None, 0);
			}
		}
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
	    var collisionPoint8 = 0f;
	    var num10 = Projectile.ai[0] / 25f;
	    if (num10 > 1f)
	    {
		    num10 = 1f;
	    }
	    var num11 = (Projectile.ai[0] - 38f) / 40f;
	    if (num11 < 0f)
	    {
		    num11 = 0f;
	    }
	    Vector2 lineStart = Projectile.Center + Projectile.rotation.ToRotationVector2() * 400f * num11;
	    Vector2 lineEnd = Projectile.Center + Projectile.rotation.ToRotationVector2() * 400f * num10;
	    if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), lineStart, lineEnd, 40f * Projectile.scale, ref collisionPoint8))
	    {
		    return true;
	    }
	    return false;
    }
}