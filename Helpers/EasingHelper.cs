using System;
using Luminance.Common.Easings;

namespace AcidicBosses.Helpers;

/// <summary>
/// Easing functions made using the equations on https://easings.net/
/// </summary>
public static class EasingHelper
{
    public static float SineIn(float x) => 1 - MathF.Cos((x * MathF.PI) / 2f);
    public static float SineOut(float x) => MathF.Sin((x * MathF.PI) / 2);
    public static float SineInOut(float x) => -(MathF.Cos(MathF.PI * x) - 1) / 2;

    public static float QuadIn(float x) => x * x;
    public static float QuadOut(float x) => 1 - (1 - x) * (1 - x);
    public static float QuadInOut(float x) => x < 0.5 ? 2 * x * x : 1 - MathF.Pow(-2 * x + 2, 2) / 2;

    public static float CubicIn(float x) => x * x * x;
    public static float CubicOut(float x) => 1 - MathF.Pow(1 - x, 3);
    public static float CubicInOut(float x) => x < 0.5 ? 4 * x * x * x : 1 - MathF.Pow(-2 * x + 2, 3) / 2;

    // Quart goes here if needed

    // Quint goes here if needed

    public static float ExpIn(float x) => x == 0 ? 0 : MathF.Pow(2, 10 * x - 10);
    public static float ExpOut(float x) => x == 1 ? 1 : 1 - MathF.Pow(2, -10 * x);
    public static float ExpInOut(float x) => x == 0 // This is what too many ternary operators does to a mf
        ? 0
        : x == 1
            ? 1
            : x < 0.5
                ? MathF.Pow(2, 20 * x - 10) / 2
                : (2 - MathF.Pow(2, -20 * x + 10)) / 2;

    public static float BackIn(float x)
    {
        var c1 = 1.70158f;
        var c3 = c1 + 1;
        
        return c3 * x * x * x - c1 * x * x;
    }
    
    public static float BackOut(float x)
    {
        var c1 = 1.70158f;
        var c3 = c1 + 1;

        return 1 + c3 * MathF.Pow(x - 1, 3) + c1 * MathF.Pow(x - 1, 2);
    }
    
    public static float BackInOut(float x)
    {
        var c1 = 1.70158f;
        var c2 = c1 * 1.525f;

        return x < 0.5
            ? (MathF.Pow(2 * x, 2) * ((c2 + 1) * 2 * x - c2)) / 2
            : (MathF.Pow(2 * x - 2, 2) * ((c2 + 1) * (x * 2 - 2) + c2) + 2) / 2;
    }
}