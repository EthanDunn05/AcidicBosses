﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AcidicBosses.Core.StateManagement;

/// <summary>
/// A class for managing the phases of a boss.
/// </summary>
/// <remarks>
/// Don't forget to call <see cref="Serialize"/> and <see cref="Deserialize"/>
/// to work with networking.
/// </remarks>
public class PhaseTracker
{
    private PhaseState[] phases;
    private int currentStateIndex = 0;

    private bool toCallEnter = false;
    
    /// <summary>
    /// The current phase
    /// </summary>
    public PhaseState CurrentPhase => phases[currentStateIndex];

    /// <summary>
    /// Creates the phase tracker with a list of phases. Phases are progressed
    /// through in order first to last.
    /// </summary>
    /// <param name="phases">The list of phases to use</param>
    public PhaseTracker(PhaseState[] phases)
    {
        this.phases = phases;
        CurrentPhase.EnterTransition?.Invoke();
    }

    /// <summary>
    /// Executes the current phase AI.
    /// This is designed to be run in the boss's AI method.
    /// </summary>
    public void RunPhaseAI()
    {
        if (toCallEnter)
        {
            CurrentPhase.EnterTransition?.Invoke();
            toCallEnter = false;
        }
        
        CurrentPhase.PhaseAI.Invoke();
    }

    /// <summary>
    /// Moves to the next phase
    /// </summary>
    /// <exception cref="IndexOutOfRangeException">
    /// Thrown when trying to start the next phase while on the last phase
    /// </exception>
    public void NextPhase()
    {
        CurrentPhase.ExitTransition?.Invoke();
        currentStateIndex++;
        if (currentStateIndex >= phases.Length) 
            throw new IndexOutOfRangeException("Cannot move to the next phase as it is not defined.");
        
        toCallEnter = true;
    }

    /// <summary>
    /// Saves phase data to the network
    /// </summary>
    /// <param name="writer"></param>
    public void Serialize(BinaryWriter writer)
    {
        writer.Write7BitEncodedInt(currentStateIndex);
    }

    /// <summary>
    /// Loads phase data from the network
    /// </summary>
    /// <param name="reader"></param>
    public void Deserialize(BinaryReader reader)
    {
        currentStateIndex = reader.Read7BitEncodedInt();
    }
}