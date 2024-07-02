using System;

namespace AcidicBosses.Core.StateManagement;

/// <summary>
/// Represents a single attack of a boss.
/// </summary>
/// <param name="attackBehavior">
/// The Function of the attack.
/// The function returns true when the attack is finished.
/// </param>
/// <param name="windDown">The delay before the next attack</param>
public struct AttackState(Func<bool> attackBehavior, int windDown)
{
    public readonly Func<bool> AttackBehavior = attackBehavior;
    public readonly int WindDown = windDown;

    /// <summary>
    /// An action to be preformed once the attack ends.
    /// </summary>
    public Action OnDone;
}