using System;

namespace AcidicBosses.Core.StateManagement;

/// <summary>
/// A representation of a boss phase.
/// </summary>
/// <param name="phaseAI">The AI function to be preformed every frame</param>
/// <param name="enterTransition">An optional function that executes when this phase becomes active</param>
/// <param name="exitTransition">An optional function that executes when this phase is no longer active</param>
public struct PhaseState(Action phaseAI, Action enterTransition = null, Action exitTransition = null)
{
    public readonly Action PhaseAI = phaseAI;
    public readonly Action EnterTransition = enterTransition;
    public readonly Action ExitTransition = exitTransition;
}