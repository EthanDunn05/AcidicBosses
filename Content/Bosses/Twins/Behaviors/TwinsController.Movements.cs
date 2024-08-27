using AcidicBosses.Content.Particles.Animated;
using AcidicBosses.Core.StateManagement;
using AcidicBosses.Helpers.NpcHelpers;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace AcidicBosses.Content.Bosses.Twins;

public partial class TwinsController
{
    private void Hover(Twin twin, float speed, float accel)
    {
        var target = Main.player[NPC.target];
        if (twin == Spazmatism)
        {
            var spazOffset = new Vector2(-200, 0);
            if (Spazmatism.Npc.Center.X > target.Center.X) spazOffset = new Vector2(200, 0);
            FlyTo(Spazmatism.Npc, target.Center + spazOffset, speed, accel);
            Spazmatism.LookTowards(target.Center, 0.05f);
        }
        else
        {
            var retOffset = new Vector2(0, -250);
            if (Retinazer.Npc.Center.Y > target.Center.Y) retOffset = new Vector2(0, 250);
            FlyTo(Retinazer.Npc, target.Center + retOffset, speed, accel);
            Retinazer.LookTowards(target.Center, 0.05f);
        }
    }

    private void Hover(float speed, float accel)
    {
        Hover(Spazmatism, speed, accel);
        Hover(Retinazer, speed, accel);
    }
    
    private bool Attack_Hover(int hoverTime, float speed, float acceleration)
    {
        attackManager.CountUp = true;

        Hover(speed, acceleration);

        if (attackManager.AiTimer <= hoverTime) return false;

        attackManager.CountUp = false;
        return true;
    }
    
    private bool Recenter()
    {
        var target = Main.player[NPC.target];
        
        var spazOffset = new Vector2(-400, 0);
        if (Spazmatism.Npc.Center.X > target.Center.X) spazOffset = new Vector2(400, 0);
        Teleport(Spazmatism, target.Center + spazOffset, 20);
        Spazmatism.LookTowards(target.Center, 0.05f);
        
        var retOffset = new Vector2(0, -450);
        // if (Retinazer.Npc.Center.Y > target.Center.Y) retOffset = new Vector2(0, 450);
        Teleport(Retinazer, target.Center + retOffset, 20);
        Retinazer.LookTowards(target.Center, 0.05f);

        return true;
    }
    
    private void Teleport(Twin twin, Vector2 position, float recoil)
    {
        var npc = twin.Npc;
        var awayDir = npc.DirectionTo(position);
        var startPos = npc.Center;

        npc.rotation = awayDir.ToRotation() - MathHelper.PiOver2;
        DashFx(twin);
        
        if (Main.netMode != NetmodeID.MultiplayerClient)
        {
            NewAfterimage(twin, startPos, position);
        }
        
        npc.Center = position;
        npc.velocity = awayDir * recoil;

        var disperse = new BigSmokeDisperseParticle(npc.Center, Vector2.Zero, 0f, Color.WhiteSmoke, 30);
        disperse.Scale *= 2f;
        disperse.Opacity = 0.5f;
        disperse.Spawn();
    }
    
    private DashState Dash(Twin twin, AttackManager am, Vector2 dashTarget, DashOptions options)
    {
        var npc = twin.Npc;
        var state = DashHelper.Dash(twin.Npc, am, dashTarget, options);
        
        if (am.AiTimer == 0)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                NewDashLine(twin, npc.Center, MathHelper.PiOver2, options.DashAtTime);
            }
        }
        
        // Lots of FX
        if (state == DashState.StartingDash)
        {
            DashFx(twin);
            twin.UseAfterimages = true;
        }
        
        if (state == DashState.Done)
        {
            twin.UseAfterimages = false;
        }

        return state;
    }

    // Dash effects
    private void DashFx(Twin twin)
    {
        var npc = twin.Npc;
        
        // Smoke ring
        var ring = new SmokeRingParticle(npc.Center, -npc.velocity * 0.25f, npc.rotation, Color.White, 30);
        ring.Opacity = 0.5f;
        ring.Scale *= 2f;
        ring.Spawn();

        // Sparks
        for (var i = 0; i < 20; i++)
        {
            var dustId = twin is Spazmatism ? DustID.CursedTorch : DustID.CrimsonTorch;
            var velOffset = Main.rand.NextVector2Circular(10f, 10f);
                
            Dust.NewDustDirect(npc.Center, 0, 0, dustId, 
                -(npc.velocity.X * 0.75f) + velOffset.X,
                -(npc.velocity.Y * 0.75f) + velOffset.Y, Scale: 1.5f);
        }
            
        // Smoke dust
        for (var i = 0; i < 20; i++)
        {
            var velOffset = Main.rand.NextVector2Circular(15f, 15f);
            Dust.NewDustDirect(npc.Center, 0, 0, DustID.Smoke, 
                -(npc.velocity.X * 0.75f) + velOffset.X,
                -(npc.velocity.Y * 0.75f) + velOffset.Y, Scale: 2f);
        }

        // Lighting
        var color = twin is Spazmatism ? Color.Lime : Color.Red;
        Lighting.AddLight(npc.Center - npc.velocity * 5f, color.ToVector3());
            
        SoundEngine.PlaySound(SoundID.Item89, npc.Center);
    }
    
    private static void FlyTo(NPC npc, Vector2 target, float speed, float acceleration)
    {
        var distanceLerp = Utils.GetLerpValue(400, 200, npc.Distance(target), true);
        var spd = MathHelper.Lerp(speed, 1f, distanceLerp);
        var accel = MathHelper.Lerp(acceleration, acceleration * 2, distanceLerp);
        npc.SimpleFlyMovement(npc.DirectionTo(target) * spd, accel);
    }
}