using Luminance.Common.Easings;

namespace AcidicBosses.Helpers;

public static class MoreEasingCurves
{
    public static EasingCurves.Curve Back = new(EasingHelper.BackIn, EasingHelper.BackOut, EasingHelper.BackInOut);
}