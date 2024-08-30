using System;
using Luminance.Common.Utilities;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace AcidicBosses.Content.Particles;

public abstract class BetterParticle : Particle
{
    private bool doneFirstFrame = false;
    
    /// <summary>
    /// Should the particle ignore lighting conditions
    /// </summary>
    public bool IgnoreLighting = false;

    /// <summary>
    /// The angular velocity of the particle
    /// </summary>
    public float AngularVelocity = 0;

    public Action<BetterParticle>? OnUpdate;
    
    public BetterParticle(Vector2 position, Vector2 velocity, float rotation, Color color, int lifetime)
    {
        Position = position;
        Velocity = velocity;
        Rotation = rotation;
        DrawColor = IgnoreLighting ? color : color.MultiplyRGB(Lighting.GetColor(position.ToTileCoordinates()));
        Lifetime = lifetime;
        Scale = new Vector2(2f, 2f); // 2x scale matches Terraria's pixel size
    }

    public virtual void FirstFrame()
    {
        
    }
    
    public override void Update()
    {
        base.Update();
        if (!doneFirstFrame)
        {
            doneFirstFrame = true;
            FirstFrame();
        }
        
        Rotation = MathHelper.WrapAngle(Rotation + AngularVelocity);
        OnUpdate?.Invoke(this);
    }
}