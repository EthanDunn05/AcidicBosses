using System;
using System.Linq;
using AcidicBosses.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;

namespace AcidicBosses.Core.Graphics.Sprites;

public class EffectLine
{
    private bool doneFirstFrame = false;
    protected int frameCounter = 0;

    public Asset<Texture2D> Texture;
    
    public Vector2 Position;
    public Vector2 Velocity = Vector2.Zero;
    public float Rotation;
    public float AngularVelocity;
    public float Length;
    public float Width;

    public Color DrawColor;
    public float Opacity = 1f;
    public Rectangle? Frame;
    public int Direction;

    public int Time;
    public int Lifetime;
    
    public Action<EffectLine>? OnUpdate;

    public float LifetimeRatio => Time / (float)Lifetime;

    public EffectLine(Asset<Texture2D> texture, Vector2 position, float rotation, float length, float width, Color color, int lifetime)
    {
        Texture = texture;
        Position = position;
        Rotation = rotation;
        Length = length;
        Width = width;
        DrawColor = color;
        Lifetime = lifetime;
    }

    public EffectLine Spawn()
    {
        if (!AcidUtils.IsClient()) return this;

        Time = 0;

        if (SpriteSystem.EffectLines.Count > 100)
        {
            SpriteSystem.EffectLines.First().Kill();
        }
        
        SpriteSystem.EffectLines.Add(this);
        return this;
    }

    public void Kill() => Time = Lifetime;

    public virtual void FirstFrame()
    {
    }

    public virtual void Update()
    {
        if (!doneFirstFrame)
        {
            doneFirstFrame = true;
            FirstFrame();
        }

        Position += Velocity;
        Rotation = MathHelper.WrapAngle(Rotation + AngularVelocity);
        OnUpdate?.Invoke(this);
        
        FindFrame();
        
        Time++;
    }

    public virtual void FindFrame()
    {
    }

    public virtual void Draw(SpriteBatch sb)
    {
        var tex = Texture.Value;
        var rect = Frame ?? tex.Frame();
        var origin = new Vector2(rect.Width / 2f, 0);
        var scale = new Vector2(Width * 2, Length) / rect.Size();
        
        sb.Draw(
            tex,
            Position - Main.screenPosition,
            rect,
            DrawColor with { A = 0 },
            Rotation - MathHelper.PiOver2,
            origin,
            scale,
            Direction == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
            0f
        );
    }
}