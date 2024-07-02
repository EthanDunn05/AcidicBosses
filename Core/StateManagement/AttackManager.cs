using System;
using System.IO;

namespace AcidicBosses.Core.StateManagement;

/// <summary>
/// A common class for managing complex attack data in npcs
/// </summary>
public class AttackManager
{
    private AttackState[] attackPattern;
    private int currentStateIndex = 0;

    /// <summary>
    /// A timer that decreases or increases every frame. Decreases if <see cref="CountUp"/> is true.
    /// </summary>
    public int AiTimer { get; set; } = 0;
    
    /// <summary>
    /// Controls if <see cref="AiTimer"/> is decreasing or increasing.
    /// </summary>
    public bool CountUp { get; set; } = false;
    
    /// <summary>
    /// The current attack being used in the current attack pattern
    /// </summary>
    public AttackState CurrentAttackState => attackPattern[currentStateIndex];

    /// <summary>
    /// Does logic that takes place before most NPC logic.
    /// This is called in AcidicNpcOverride, so don't call this manually
    /// </summary>
    public void PreAttackAi()
    {
        if (!CountUp) AiTimer = Math.Max(AiTimer - 1, 0);
    }

    /// <summary>
    /// Does logic that takes place after most NPC logic.
    /// This is called in AcidicNpcOverride, so don't call this manually
    /// </summary>
    public void PostAttackAi()
    {
        if (CountUp) AiTimer++;
    }

    /// <summary>
    /// Resets this attack manager.
    /// This includes clearing the current attack pattern.
    /// </summary>
    public void Reset()
    {
        AiTimer = 0;
        CountUp = false;
        attackPattern = null;
        currentStateIndex = 0;
    }

    /// <summary>
    /// Sets the current attack pattern to be executed from first to last repeating.
    /// </summary>
    /// <param name="attackPattern">The attack pattern</param>
    public void SetAttackPattern(AttackState[] attackPattern)
    {
        this.attackPattern = attackPattern;
    }

    /// <summary>
    /// Executes the current attack. This should be called every frame to update attacks.
    /// </summary>
    /// <exception cref="NullReferenceException">Thrown when there is no attack pattern currently set.</exception>
    public void RunAttackPattern()
    {
        if (attackPattern == null) throw new NullReferenceException("Attack pattern is currently null.");

        var isDone = CurrentAttackState.AttackBehavior.Invoke();
        if (!isDone) return;
        
        AiTimer = CurrentAttackState.WindDown;
        CurrentAttackState.OnDone?.Invoke();
        NextAttack();
    }

    /// <summary>
    /// Moves to the next attack in the attack pattern. You usually don't need to call this as
    /// the attack pattern progresses automatically.
    /// </summary>
    public void NextAttack()
    {
        currentStateIndex = (currentStateIndex + 1) % attackPattern.Length;
    }

    /// <summary>
    /// Saves the state of the manager to networking.
    /// </summary>
    public void Serialize(BinaryWriter writer)
    {
        writer.Write7BitEncodedInt(currentStateIndex);
        writer.Write7BitEncodedInt(AiTimer);
        writer.Write(CountUp);
    }

    /// <summary>
    /// Loads the state of the manager from networking.
    /// </summary>
    public void Deserialize(BinaryReader reader)
    {
        currentStateIndex = reader.Read7BitEncodedInt();
        AiTimer = reader.Read7BitEncodedInt();
        CountUp = reader.ReadBoolean();
    }
}