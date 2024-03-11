using Terraria.Localization;

namespace AcidicBosses.Helpers;

public static class ModLanguage
{
    public static LocalizedText GetText(string key)
    {
        return Language.GetText("Mods.AcidicBosses." + key);
    }
}