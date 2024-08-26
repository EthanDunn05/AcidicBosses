using System;
using Luminance.Common.Utilities;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace AcidicBosses.Content.Particles;

/// <summary>
/// An animated particle. A texture for a particle should be horizontal and not trimmed
/// </summary>
public abstract class AnimatedParticle : Particle
{
    private int frame = 0;
    private bool doneFirstFrame = false;
    public override int FrameCount => Texture.Width / FrameWidth * Texture.Height / FrameHeight;
    
    /// <summary>
    /// The width of one frame of animation
    /// </summary>
    protected abstract int FrameWidth { get; }

    protected virtual int FrameHeight => FrameWidth;
    
    /// <summary>
    /// Should the animation loop? Default is false
    /// </summary>
    public bool Looping = false;

    /// <summary>
    /// The game ticks between each frame. Default is 2
    /// </summary>
    public int FrameInterval = 2;

    public bool IgnoreLighting = false;

    public float AngularVelocity = 0;

    public Action<AnimatedParticle>? OnUpdate = null;
    
    public AnimatedParticle(Vector2 position, Vector2 velocity, float rotation, Color color, int lifetime)
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
        if (!doneFirstFrame)
        {
            doneFirstFrame = true;
            FirstFrame();
        }
        
        OnUpdate?.Invoke(this);

        Rotation = MathHelper.WrapAngle(Rotation + AngularVelocity);
        
        if (frame >= FrameCount)
        {
            frame = 0;
            if (!Looping) Kill();
        }

        var horizFrames = Texture.Frame.Width / FrameWidth;
        var pos = new Vector2(FrameWidth * (frame % horizFrames), FrameHeight * (frame / horizFrames));
        Frame = new Rectangle((int) (pos.X + Texture.Frame.X), (int) (pos.Y + Texture.Frame.Y), FrameWidth, FrameHeight);
        
        if (Time % FrameInterval == 0) frame++;
    }
}