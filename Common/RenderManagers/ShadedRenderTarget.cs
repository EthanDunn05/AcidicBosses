using System;
using Luminance.Core.Graphics;

namespace AcidicBosses.Common.RenderManagers;

public enum RenderLayer
{
    Projectile,
    Npc
}

public class ShadedRenderTarget : ManagedRenderTarget
{
    private Action<ShadedRenderTarget>? applyShader;
    public RenderLayer Layer;
    
    public ShadedRenderTarget(bool shouldResetUponScreenResize, RenderTargetInitializationAction creationCondition, Action<ShadedRenderTarget>? applyShader, RenderLayer layer, bool subjectToGarbageCollection = true) 
        : base(shouldResetUponScreenResize, creationCondition, subjectToGarbageCollection)
    {
        this.applyShader = applyShader;
        Layer = layer;
    }

    public void ApplyShader()
    {
        applyShader?.Invoke(this);
    }
}