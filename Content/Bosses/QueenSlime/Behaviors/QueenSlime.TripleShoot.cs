using AcidicBosses.Common.Textures;
using AcidicBosses.Core.Graphics.Sprites;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;

namespace AcidicBosses.Content.Bosses.QueenSlime;

public partial class QueenSlime
{
    private bool Attack_TripleShoot(float spread, bool fallAfter)
    {
        ref var setAngle = ref Npc.localAI[0];
        AttackManager.CountUp = true;

        if (AttackManager.AiTimer == 0)
        {
            if (!grounded)
            {
                flapping = true;
                Npc.noGravity = true;
            }
            
            Npc.velocity = Vector2.Zero;
            
            setAngle = Npc.DirectionTo(TargetPlayer.Center).ToRotation();
            SoundEngine.PlaySound(WobbleSound, Npc.Center);
            for (var i = -1; i <= 1; i++)
            {
                var angle = setAngle + (spread * i);

                new FadingEffectLine(
                    TextureRegistry.InvertedFadingGlowLine,
                    Npc.Center,
                    angle,
                    500, 10,
                    Color.Violet,
                    60
                ).Spawn();
            }
        }

        if (AttackManager.AiTimer == 60)
        {
            for (var i = -1; i <= 1; i++)
            {
                var angle = setAngle + (spread * i);
                NewRoyalGel(Npc.Center, angle.ToRotationVector2() * 25f);
            }
            
            Npc.velocity = setAngle.ToRotationVector2() * -10f;
            squash = -0.25f;

            if (!fallAfter)
            {
                AttackManager.CountUp = false;
                return true;
            }
            else
            {
                flapping = false;
                grounded = false;
                Npc.noGravity = false;
            }
        }

        if (AttackManager.AiTimer >= 60 && grounded)
        {
            AttackManager.CountUp = false;
            return true;
        }

        return false;
    }
}