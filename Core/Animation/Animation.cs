using System;
using System.Collections.Generic;

namespace AcidicBosses.Core.Animation;

/// <summary>
/// A system to help manage an animation.
/// An animation is divided into <see cref="TimedAnimationEvent"/> which are executed as the animation progresses.
/// </summary>
public class Animation
{
    public delegate void TimedAnimationEvent(float eventProgress, int eventFrame);
    public delegate void InstantAnimationEvent();
    
    public record TimingData(int StartTime, int EndTime);
    
    private Dictionary<TimingData, TimedAnimationEvent> timedEvents = new();
    private Dictionary<int, InstantAnimationEvent> instantEvents = new();
    private List<TimedAnimationEvent> constantEvents = new();
    
    private int animationEndTime = -1;
    private int animationTime = 0;

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
    /// <returns>Whether the animation is done</returns>
    public bool RunAnimation()
    {
        foreach (var (data, evt) in timedEvents)
        {
            if (data.StartTime > animationTime || animationTime >= data.EndTime) continue;
            
            var eventLength = data.EndTime - data.StartTime;
            var eventTime = animationTime - data.StartTime;
            var eventProgress = (float) eventTime / eventLength;

            evt(eventProgress, eventTime);
        }

        foreach (var (time, evt) in instantEvents)
        {
            if (animationTime != time) continue;
            
            evt();
        }

        foreach (var evt in constantEvents)
        {
            var progress = (float) animationTime / animationEndTime;
            evt(progress, animationTime);
        }

        animationTime++;
        return animationTime >= animationEndTime;
    }
}