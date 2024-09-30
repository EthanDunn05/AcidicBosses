using Microsoft.Xna.Framework;

namespace AcidicBosses.Content.Particles.Animated;

/// <summary>
/// An animated particle. A texture for a particle should be horizontal and not trimmed
/// </summary>
public abstract class AnimatedParticle : BetterParticle
{
    private int frame = 0;
    
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
    
    protected AnimatedParticle(Vector2 position, Vector2 velocity, float rotation, Color color, int lifetime) : base(position, velocity, rotation, color, lifetime)
    {
    }

    public override void Update()
    {
        base.Update();
        
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