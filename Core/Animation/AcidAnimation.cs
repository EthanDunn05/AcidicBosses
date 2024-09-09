using System;
using System.Collections.Generic;
using System.Linq;
using AcidicBosses.Helpers;
using Stubble.Core.Imported;

namespace AcidicBosses.Core.Animation;

/// <summary>
/// A system to help manage an animation.
/// An animation is divided into <see cref="TimedAnimationEvent"/> which are executed as the animation progresses.
/// All measurements of time are in frames.
/// </summary>
public class AcidAnimation
{
    public delegate void TimedAnimationEvent(float eventProgress, int eventFrame);
    public delegate void InstantAnimationEvent();
    
    public record TimingData(int StartTime, int EndTime);

    public record SequencedEvent(int Length, TimedAnimationEvent Evt);
    
    private Dictionary<TimingData, TimedAnimationEvent> timedEvents = new();
    private Dictionary<int, InstantAnimationEvent> instantEvents = new();
    private List<TimedAnimationEvent> constantEvents = [];
    
    private int animationEndTime = -1;
    private int animationTime = 0;

    /// <summary>
    /// A generic dictionary that can hold any data to be used throughout an animation.
    /// This is very versatile and can be used to track any sort of data or changed before playing the animation.
    /// </summary>
    public GenericDictionary Data = new();
    
    /// <summary>
    /// Adds a new event to the animation. The event takes place while <c>startTime &lt;= time &lt; endTime</c>.
    /// </summary>
    /// <param name="startTime">The time in which the event starts</param>
    /// <param name="endTime">The time the animation ends. This frame is not included in the event</param>
    /// <param name="eventAction">The action to take place each frame the event is active</param>
    public void AddTimedEvent(int startTime, int endTime, TimedAnimationEvent eventAction)
    {
        if (animationEndTime < endTime) animationEndTime = endTime;
        
        timedEvents.Add(new TimingData(startTime, endTime), eventAction);
    }
    
    /// <summary>
    /// Adds a new event that starts at the end of the last timed event and lasts a certain amount of time.
    /// </summary>
    /// <param name="length">The length of the new event</param>
    /// <param name="eventAction">The action to take place each frame the event is active</param>
    /// <returns>The timing data for the event</returns>
    public TimingData AddSequencedEvent(int length, TimedAnimationEvent eventAction)
    {
        var timing = new TimingData(animationEndTime, animationEndTime + length);
        timedEvents.Add(timing, eventAction);
        animationEndTime += length;

        return timing;
    }

    /// <summary>
    /// Adds an event that happens on an exact frame of animation
    /// </summary>
    /// <param name="time">The time the event happens</param>
    /// <param name="eventAction">The action of the event</param>
    public void AddInstantEvent(int time, InstantAnimationEvent eventAction)
    {
        if (animationEndTime < time + 1) animationEndTime = time + 1;
        instantEvents.Add(time, eventAction);
    }

    /// <summary>
    /// Adds an event which plays through the entire animation
    /// </summary>
    /// <param name="eventAction">The event to play</param>
    public void AddConstantEvent(TimedAnimationEvent eventAction)
    {
        constantEvents.Add(eventAction);
    }

    /// <summary>
    /// Runs the animation. Must be called every frame.
    /// </summary>
    /// <remarks>
    /// Event types are performed in the following order:
    /// <code>
    /// 1. Instant
    /// 2. Timed
    /// 3. Constant
    /// </code>
    /// </remarks>
    /// <returns>Whether the animation is done</returns>
    public bool RunAnimation()
    {
        foreach (var (time, evt) in instantEvents)
        {
            if (animationTime != time) continue;
            
            evt();
        }
        
        foreach (var (data, evt) in timedEvents)
        {
            if (data.StartTime > animationTime || animationTime >= data.EndTime) continue;
            
            var eventLength = data.EndTime - data.StartTime;
            var eventTime = animationTime - data.StartTime;
            var eventProgress = (float) eventTime / eventLength;

            evt(eventProgress, eventTime);
        }

        foreach (var evt in constantEvents)
        {
            var progress = (float) animationTime / animationEndTime;
            evt(progress, animationTime);
        }

        animationTime++;
        return animationTime >= animationEndTime;
    }
    
    /// <summary>
    /// Resets the animation. Does NOT clear any events
    /// </summary>
    public void Reset()
    {
        animationTime = 0;
        Data.Clear();
    }
}