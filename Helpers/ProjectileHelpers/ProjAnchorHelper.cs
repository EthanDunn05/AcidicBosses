using Microsoft.Xna.Framework;
using Terraria;

namespace AcidicBosses.Helpers.ProjectileHelpers;

/// <summary>
/// A projectile implementing this interface can be anchored to an NPC.
/// </summary>
public interface IAnchoredProjectile
{
    /// <summary>
    /// The rotation offset from the owner's rotation
    /// </summary>
    public float Offset { get; }
    
    /// <summary>
    /// The id of the NPC to anchor to
    /// </summary>
    public int AnchorTo { get; }
    
    /// <summary>
    /// Should this projectile anchor to the NPC's position
    /// </summary>
    public bool AnchorPosition { get; }
    
    /// <summary>
    /// Should this projectile anchor to the NPC's rotation
    /// </summary>
    public bool AnchorRotation { get; }
    
    /// <summary>
    /// Should this projectile rotate around the center of the NPC rather than rotate in place.
    /// </summary>
    public bool RotateAroundCenter { get; }
    
    /// <summary>
    /// The start offset. This is best left untouched when implementing
    /// </summary>
    public Vector2? StartOffset { get; set; }
}

public static class ProjAnchorHelper
{
    public static void Anchor(this IAnchoredProjectile anchor, Projectile proj)
    {
        if (anchor.AnchorTo >= 0)
        {
            var owner = Main.npc[anchor.AnchorTo];
            if (owner != null)
            {
                anchor.StartOffset ??= owner.Center - proj.position;

                if (anchor.AnchorRotation) proj.rotation = owner.rotation + anchor.Offset;
                else proj.rotation = anchor.Offset;
                
                if (anchor.AnchorPosition)
                {
                    if (anchor.RotateAroundCenter && anchor.AnchorRotation)
                    {
                        var rotation = owner.rotation + anchor.Offset;
                        var offset = anchor.StartOffset.Value.Length() * rotation.ToRotationVector2();
                        proj.position = owner.Center + offset;
                    }
                    else
                    {
                        proj.position = (Vector2) (owner.Center + anchor.StartOffset)!;
                    }
                }

            }
        }
        else
        {
            proj.rotation = anchor.Offset;
        }
    }
}