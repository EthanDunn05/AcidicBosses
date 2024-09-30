using System;
using AcidicBosses.Common;
using AcidicBosses.Common.Textures;
using AcidicBosses.Content.Particles;
using AcidicBosses.Content.ProjectileBases;
using AcidicBosses.Core.Animation;
using AcidicBosses.Helpers;
using Luminance.Common.Utilities;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Utils = Microsoft.VisualBasic.CompilerServices.Utils;

namespace AcidicBosses.Content.Bosses.Twins.Projectiles;

public class SpazCircleIndicator : BaseEffectProjectile
{
    private AcidAnimation anim;
    private float size = 0f;
    private float width = 0f;
    
    public override void AI()
    {
        base.AI();
        anim.RunAnimation();
    }

    public override void FirstFrame()
    {
        base.FirstFrame();
        anim = new AcidAnimation();
        
        anim.AddTimedEvent(0, 30, (progress, frame) =>
        {
            var ease = EasingHelper.BackOut(progress);
            Projectile.Opacity = MathHelper.Lerp(0f, 1f, ease);
            size = MathHelper.Lerp(0f, 600f, ease);
            width = MathHelper.Lerp(0f, 100f, ease);
        });
        
        anim.AddTimedEvent(30, MaxTimeLeft, (progress, frame) =>
        {
            var ease = EasingHelper.QuadIn(progress);
            Projectile.Opacity = MathHelper.Lerp(1f, 0f, ease);
        });
    }

    public override bool PreDraw(ref Color lightColor)
    {
        var circleSettings = new PrimitiveSettingsCircleEdge(
            _ => width, 
            _ => Color.Lime * Projectile.Opacity, 
            _ => MathF.Max(0f, size - width / 2f), 
            Shader: ShaderManager.GetShader("AcidicBosses.Rope")
        );
        
        circleSettings.Shader.SetTexture(TextureRegistry.SideGlowLine, 1, SamplerState.PointClamp);
        circleSettings.Shader.TrySetParameter("segments", 1);
        PrimitiveRenderer.RenderCircleEdge(Projectile.position, circleSettings);

        Main.spriteBatch.EnterShader(BlendState.Additive);
        var fireballs = 10;
        for (var i = 0; i < fireballs / 2; i++)
        {
            var fireballProgress = (float) i / fireballs;
                
            var angle = MathHelper.TwoPi * fireballProgress;
            var fireX = MathF.Cos(angle) * size;
            var fireY = MathF.Sin(angle) * size;
            var pos1 = Projectile.position + new Vector2(fireX, fireY);
            var pos2 = Projectile.position - new Vector2(fireX, fireY);

            Main.spriteBatch.DrawBloomLine(pos1, pos2, Color.Lime * Projectile.Opacity, 25f);          
        }

        Main.spriteBatch.ExitShader();
        
        return false;
    }
}