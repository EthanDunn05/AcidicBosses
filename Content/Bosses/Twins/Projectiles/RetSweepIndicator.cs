using AcidicBosses.Common.Textures;
using AcidicBosses.Content.ProjectileBases;
using AcidicBosses.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;

namespace AcidicBosses.Content.Bosses.Twins.Projectiles;

public class RetSweepIndicator : BaseSweep
{
    protected override float Length { get; set; } = 12000;
    protected override float Width { get; set; } = 25;
    protected override Color Color { get; set; } = Color.Red;
    protected override float Radius { get; set; } = MathHelper.PiOver4;
    public override bool AnchorRotation => false;
    
    private Color GetColor()
    {
        var fadeT = (float) Projectile.timeLeft / MaxTimeLeft;
        var color = Color.Red;
        color *= EasingHelper.CubicOut(fadeT);
        return color;
    }

    public override void AI()
    {
        base.AI();

        Color = GetColor();

        ref var timeAlive = ref Projectile.localAI[0];
        
        var inTime = 30;
        if (timeAlive < inTime)
        {
            var ease = EasingHelper.BackOut((float) timeAlive / inTime);
            Width = MathHelper.Lerp(0, 25, ease);
            Radius = MathHelper.Lerp(0f, MathHelper.PiOver4, ease);
        }

        timeAlive++;
    }
}