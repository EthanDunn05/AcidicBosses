using AcidicBosses.Content.Particles.Animated;
using AcidicBosses.Core.Animation;
using Luminance.Common.Utilities;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace AcidicBosses.Content.Bosses.Twins;

public partial class TwinsController
{
    private AcidAnimation transformAnimation = new();

    private void CreateTransformationAnimation()
    {
        transformAnimation = new AcidAnimation();
        
        // Stop Moving
        transformAnimation.AddConstantEvent((progress, frame) =>
        {
            Spazmatism.Npc.SimpleFlyMovement(Vector2.Zero, 0.2f);
            Retinazer.Npc.SimpleFlyMovement(Vector2.Zero, 0.2f);
        });
        
        // Start rumble
        transformAnimation.AddInstantEvent(0, () =>
        {
            ScreenShakeSystem.SetUniversalRumble(0.5f, shakeStrengthDissipationIncrement: 0f);
        });

        // Make Smoke
        transformAnimation.AddTimedEvent(0, 120, (progress, frame) =>
        {
            DoBothTwins(twin =>
            {
                var npc = twin.Npc;
                if (frame % 30 == 0)
                {
                    SoundEngine.PlaySound(SoundID.Item13, npc.Center);
                }

                var smokeDusts = MathHelper.Lerp(1f, 4f, progress);
                for (var i = 0; i < smokeDusts; i++)
                {
                    var speed = Main.rand.NextVector2Circular(5, 5);
                    var pos = Main.rand.NextVector2Circular(npc.width / 2f, npc.height / 2f) + npc.Center;
                    Dust.NewDust(pos, 0, 0, DustID.Smoke, speed.X, speed.Y, Scale: 1.5f);
                }
                if (Main.rand.NextBool(smokeDusts % 1f))
                {
                    var speed = Main.rand.NextVector2Circular(5, 5);
                    var pos = Main.rand.NextVector2Circular(npc.width / 2f, npc.height / 2f) + npc.Center;
                    Dust.NewDust(pos, 0, 0, DustID.Smoke, speed.X, speed.Y, Scale: 1.5f);
                }
                
                var smokeParticles = MathHelper.Lerp(0f, 1f, progress);
                for (var i = 0; i < smokeParticles; i++)
                {
                    var speed = Main.rand.NextVector2Circular(2.5f, 2.5f);
                    var pos = Main.rand.NextVector2Circular(npc.width / 2f, npc.height / 2f) + npc.Center;
                    var rot = Main.rand.NextFloatDirection();
                    var angularVel = Main.rand.NextFloat(0f, MathHelper.Pi / 16f);
                    var puff = new SmallPuffParticle(pos, speed, rot, Color.WhiteSmoke, 30);
                    puff.Opacity = 0.5f;
                    puff.AngularVelocity = angularVel;
                    puff.Spawn();
                }
                if (Main.rand.NextBool(smokeParticles % 1f))
                {
                    var speed = Main.rand.NextVector2Circular(2.5f, 2.5f);
                    var pos = Main.rand.NextVector2Circular(npc.width / 2f, npc.height / 2f) + npc.Center;
                    var rot = Main.rand.NextFloatDirection();
                    var angularVel = Main.rand.NextFloat(0f, MathHelper.Pi / 16f);
                    var puff = new SmallPuffParticle(pos, speed, rot, Color.WhiteSmoke, 30);
                    puff.Opacity = 0.5f;
                    puff.AngularVelocity = angularVel;
                    puff.Spawn();
                }
            });
        });
        
        // Transform twins
        transformAnimation.AddInstantEvent(120, () =>
        {
            ScreenShakeSystem.SetUniversalRumble(0f, shakeStrengthDissipationIncrement: 0.2f);
            ScreenShakeSystem.StartShake(2f);
            
            DoBothTwins(twin =>
            {
                var npc = twin.Npc;
            
                SoundEngine.PlaySound(SoundID.NPCHit1, npc.Center);
                SoundEngine.PlaySound(SoundID.Roar, npc.Center);

                var burst = new BigSmokeDisperseParticle(npc.Bottom, Vector2.Zero, 0f, Color.WhiteSmoke, 60);
                burst.IgnoreLighting = true;
                burst.Scale *= 2.25f;
                burst.Spawn();
            
                for (var i = 0; i < 2; i++)
                {
                    Gore.NewGore(NPC.GetSource_FromAI(), npc.Center, Main.rand.NextVector2Square(-30, 31) * 0.2f, 144);
                    Gore.NewGore(NPC.GetSource_FromAI(), npc.Center, Main.rand.NextVector2Square(-30, 31) * 0.2f, 7);
                    Gore.NewGore(NPC.GetSource_FromAI(), npc.Center, Main.rand.NextVector2Square(-30, 31) * 0.2f, 6);
                }
                for (var i = 0; i < 20; i++)
                {
                    Dust.NewDust(npc.position, npc.width, npc.height, DustID.Blood, Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f);
                }
                
                var dustId = twin is Spazmatism ? DustID.CursedTorch : DustID.CrimsonTorch;
                for (var i = 0; i < 20; i++)
                {
                    var speed = Main.rand.NextVector2CircularEdge(15f, 15f);
                    Dust.NewDust(npc.Center, 0, 0, dustId, speed.X, speed.Y, Scale: 2f);
                }
            
                npc.HitSound = SoundID.NPCHit4;
                twin.MechForm = true;
            });
        });
    }
}