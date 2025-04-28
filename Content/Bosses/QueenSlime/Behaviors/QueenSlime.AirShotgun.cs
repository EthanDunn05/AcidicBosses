using AcidicBosses.Common.Textures;
using AcidicBosses.Core.Animation;
using AcidicBosses.Core.Graphics.Sprites;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;

namespace AcidicBosses.Content.Bosses.QueenSlime;

public partial class QueenSlime
{
    private AcidAnimation? airShotgunAnimation;

    private AcidAnimation PrepareAirShotgunAnimation()
    {
        var anim = new AcidAnimation();
        
        anim.AddInstantEvent(0, () =>
        {
            JumpTo(TargetPlayer.Center, 60);
        });
        
        anim.AddSequencedEvent(30, (progress, frame) =>
        {
            
        });
        
        var indicateTiming = anim.AddSequencedEvent(30, (progress, frame) =>
        {
            flapping = true;
            Npc.noGravity = true;
            Npc.velocity = Vector2.Lerp(Npc.velocity, Vector2.Zero, 0.25f);

            if (frame == 0)
            {
                var projectiles = anim.Data.Get<int>("projectiles");
                var spread = anim.Data.Get<float>("spread");
                
                var setAngle = Npc.DirectionTo(TargetPlayer.Center).ToRotation();
                anim.Data.Set("setAngle", setAngle);
                SoundEngine.PlaySound(WobbleSound, Npc.Center);
                for (var i = -projectiles / 2; i <= projectiles / 2; i++)
                {
                    var angle = setAngle + (spread * i / projectiles);

                    new FadingEffectLine(
                        TextureRegistry.InvertedFadingGlowLine,
                        Npc.Center,
                        angle,
                        500, 10,
                        Color.Violet,
                        30
                    )
                    {
                        OnUpdate = line =>
                        {
                            line.Position = Npc.Center;
                        }
                    }.Spawn();
                }
            }
        });
        
        anim.AddInstantEvent(indicateTiming.EndTime, () =>
        {
            var projectiles = anim.Data.Get<int>("projectiles");
            var spread = anim.Data.Get<float>("spread");
            var setAngle = anim.Data.Get<float>("setAngle");
            
            flapping = false;
            grounded = false;
            Npc.noGravity = false;
            
            for (var i = -projectiles / 2; i <= projectiles / 2; i++)
            {
                var angle = setAngle + (spread * i / projectiles);
                NewRoyalGel(Npc.Center, angle.ToRotationVector2() * 25f);
            }
            
            Npc.velocity = setAngle.ToRotationVector2() * -10f;
            squash = -0.25f;
        });

        return anim;
    }
    
    private bool Attack_AirShotgun(int projectiles, float spread, bool waitForLand)
    {
        airShotgunAnimation ??= PrepareAirShotgunAnimation();
        airShotgunAnimation.Data.Set("projectiles", projectiles);
        airShotgunAnimation.Data.Set("spread", spread);
        if (airShotgunAnimation.RunAnimation() && (grounded || !waitForLand))
        {
            airShotgunAnimation.Reset();
            return true;
        }

        return false;
    }
}