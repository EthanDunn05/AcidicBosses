using System.Linq;
using AcidicBosses.Common.Primitive;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace AcidicBosses.Helpers;

public static class ProjHelper
{
    public static void DrawPrimRay(Projectile projectile, LinePrimDrawer lineDrawer, float direction, float length)
    {
        var points = new Vector2[3];
        
        // 3 points because 2 isn't enough geometry
        for (var i = 0; i < 3; i++)
        {
            points[i] = projectile.Center + direction.ToRotationVector2() * i * length / 3f;
        }
        
        lineDrawer.Draw(points, -Main.screenPosition, 3);
    }
    
    public static Projectile FindProjectile(int identity) => Main.projectile.FirstOrDefault(p => p.identity == identity);
}