using System;
using AcidicBosses.Core.StateManagement;
using Microsoft.Xna.Framework;
using Terraria;

namespace AcidicBosses.Helpers.NpcHelpers;

// This class is highly documented and must stay highly documented.
// Many bosses depend on this class working properly as dashing is an extremely common behavior.

/// <summary>
/// The state of a dash
/// </summary>
public enum DashState
{
    /// <summary>
    /// The npc is backing away from the target to maintain a minimum distance
    /// </summary>
    Repositioning,
    
    /// <summary>
    /// The npc is actively tracking the target
    /// </summary>
    Tracking,
    
    /// <summary>
    /// The npc is idle, waiting for the dashTime
    /// </summary>
    Waiting,
    
    /// <summary>
    /// The frame the npc starts the movement of the dash
    /// </summary>
    StartingDash,
    
    /// <summary>
    /// The npc is actively dashing
    /// </summary>
    Dashing,
    
    /// <summary>
    /// The frame the dash ends
    /// </summary>
    Done
}

/// <summary>
/// All the information used during a dash.
/// </summary>
public struct DashOptions
{
    /// <summary>
    /// How long the npc dashes
    /// </summary>
    public required int DashLength;
        
    /// <summary>
    /// How fast the npc dashes
    /// </summary>
    public required float DashSpeed;
        
    /// <summary>
    /// How long the npc will track the target
    /// </summary>
    public required int TrackTime;
        
    /// <summary>
    /// How long before the npc starts dashing
    /// </summary>
    public required int DashAtTime;
        
    /// <summary>
    /// How far away from the target must the npc stay before dashing
    /// </summary>
    public required float MinimumDistance;
        
    /// <summary>
    /// The offset between the npc's sprite rotation and true rotation.
    /// Best shown for the Eye of Cthulhu which uses a look offset of Pi/2
    /// </summary>
    public required float LookOffset;

    /// <summary>
    /// Stop the npc from <see cref="DashState.Repositioning"/> before dashing.
    /// </summary>
    public bool DontReposition;
}

public static class DashHelper
{
    /// <summary>
    /// Manages the Dash of an NPC. Check <see cref="DashState"/> for what states the dash can be in.
    /// </summary>
    /// <param name="npc">The npc to be dashing</param>
    /// <param name="attackManager">An attack manager to track the timing of the dash</param>
    /// <param name="dashTarget">Where the dash should target</param>
    /// <param name="options">The options for this dash. This should never change during a dash or else you risk breaking things</param>
    /// <returns>The current state of the dash on this frame</returns>
    public static DashState Dash(NPC npc, AttackManager attackManager, Vector2 dashTarget, DashOptions options)
    {
        attackManager.CountUp = true;
        
        // FYI, these checks are in the order they happen in game //
        
        // Track the dash target
        if (attackManager.AiTimer < options.TrackTime)
        {
            // Don't dash while too close to the target
            // Back away until it's far enough
            if (!options.DontReposition && npc.Distance(dashTarget) < options.MinimumDistance)
            {
                attackManager.AiTimer = -1;
                npc.SimpleFlyMovement(-npc.DirectionTo(dashTarget) * 10f, 0.5f);
                npc.rotation = npc.rotation.AngleLerp(npc.AngleTo(dashTarget) - options.LookOffset, 0.25f);
                return DashState.Repositioning;
            }
            
            // Track the target
            npc.SimpleFlyMovement(Vector2.Zero, 0.75f);
            npc.rotation = npc.rotation.AngleLerp(npc.AngleTo(dashTarget) - options.LookOffset, 0.25f);
            return DashState.Tracking;
        }

        // Wait until the time to dash
        if (attackManager.AiTimer < options.DashAtTime)
        {
            return DashState.Waiting;
        }

        // Start dashing
        if (attackManager.AiTimer == options.DashAtTime)
        {
            npc.velocity = (npc.rotation + options.LookOffset).ToRotationVector2() * options.DashSpeed;
            return DashState.StartingDash;
        }
        
        // While moving
        if (attackManager.AiTimer < options.DashAtTime + options.DashLength)
        {
            return DashState.Dashing;
        }
        
        // Reset the attack manager when done
        attackManager.CountUp = false;
        return DashState.Done;
    }
}