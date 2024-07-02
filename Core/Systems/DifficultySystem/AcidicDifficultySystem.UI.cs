using AcidicBosses.Helpers;
using Microsoft.Xna.Framework;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.GameContent.UI.States;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace AcidicBosses.Core.Systems.DifficultySystem;

public enum AcidicEnabledID
{
    Enabled,
    Disabled,
    None
}

// UI code is so annoying to write

public partial class AcidicDifficultySystem : ModSystem
{
    private GroupOptionButton<AcidicEnabledID>[] enableOptions = new GroupOptionButton<AcidicEnabledID>[2];
    private AcidicEnabledID selectionOption = AcidicEnabledID.None;
    
    public override void Load()
    {
        On_UIWorldCreation.BuildPage += AddCustomElements;
    }

    private void AddCustomElements(On_UIWorldCreation.orig_BuildPage orig, UIWorldCreation self)
    {
        orig(self);

        // Background panel
        var difficultyPanel = new UIPanel
        {
            // These numbers are partially taken from the vanilla panel's size
            // I hate these magic numbers so much, but that's UI code for ya!
            Width = StyleDimension.FromPixels(250f),
            Height = StyleDimension.FromPixels(280f + 18),
            Left = StyleDimension.FromPixels(250f + (250f / 2f) + 10f),
            Top = StyleDimension.FromPixels(170f - 18f + 50f),
            HAlign = 0.5f,
            VAlign = 0f,
            BackgroundColor = new Color(33, 43, 79) * 0.8f
        };
        difficultyPanel.SetPadding(10f);
        self.Append(difficultyPanel);

        var accumulatedHeight = 0f;

        // Title
        var titleText = new UIText(ModLanguage.GetText("UI.AcidicToggle.Title"))
        {
            Width = StyleDimension.FromPercent(1f),
            Height = StyleDimension.FromPixels(16),
            HAlign = 0f,
            VAlign = 0f
        };
        difficultyPanel.Append(titleText);

        accumulatedHeight += titleText.Height.Pixels + 10f;
        AddHorizontalSeparator(difficultyPanel, accumulatedHeight);

        // Description
        var descriptionText = new UIText(ModLanguage.GetText("UI.AcidicToggle.Description"), 0.85f)
        {
            Width = StyleDimension.FromPercent(1f),
            Height = StyleDimension.FromPercent(1f),
            Top = StyleDimension.FromPixels(accumulatedHeight),
            HAlign = 0f,
            VAlign = 0f
        };
        descriptionText.SetSnapPoint("Description", 0);
        difficultyPanel.Append(descriptionText);

        accumulatedHeight += descriptionText.Height.Pixels + 10f;

        AddEnableOptions(difficultyPanel);

        foreach (var option in enableOptions)
        {
            option.SetCurrentOption(AcidicEnabledID.Enabled);
        }
    }

    private void AddEnableOptions(UIElement containter)
    {
        AcidicEnabledID[] options =
        {
            AcidicEnabledID.Enabled,
            AcidicEnabledID.Disabled
        };
        LocalizedText[] optionText =
        {
            ModLanguage.GetText("UI.AcidicToggle.EnableText"),
            ModLanguage.GetText("UI.AcidicToggle.DisableText")
        };
        string[] icons = new string[2]
        {
            "Images/UI/WorldCreation/IconDifficultyMaster",
            "Images/UI/WorldCreation/IconDifficultyNormal"
        };

        var optionElements = new GroupOptionButton<AcidicEnabledID>[options.Length];

        for (var i = 0; i < options.Length; i++)
        {
            var button = new GroupOptionButton<AcidicEnabledID>(
                options[i],
                optionText[i],
                Language.GetText(""),
                Color.White,
                icons[i],
                titleWidthReduction: 16f,
                titleAlignmentX: 1f
            );
            
            button.Width = StyleDimension.FromPixelsAndPercent(-1 * (options.Length - 1), 1f / (float)options.Length);
            button.Left = StyleDimension.FromPercent(0f);
            button.HAlign = (float)i / (float)(options.Length - 1);
            button.VAlign = 1f;
            button.OnLeftMouseDown += ClickEnableOption;
            
            containter.Append(button);
            optionElements[i] = button;
        }

        enableOptions = optionElements;
    }

    private void ClickEnableOption(UIMouseEvent evt, UIElement listeningElelment)
    {
        var optionButton = (GroupOptionButton<AcidicEnabledID>) listeningElelment;
        selectionOption = optionButton.OptionValue;
        foreach (var option in enableOptions)
        {
            option.SetCurrentOption(selectionOption);
        }
    }

    #region Vanilla Code

    private static void AddHorizontalSeparator(UIElement Container, float accumualtedHeight)
    {
        UIHorizontalSeparator element = new UIHorizontalSeparator
        {
            Width = StyleDimension.FromPercent(1f),
            Top = StyleDimension.FromPixels(accumualtedHeight - 8f),
            Color = Color.Lerp(Color.White, new Color(63, 65, 151, 255), 0.85f) * 0.9f
        };
        Container.Append(element);
    }

    private void FadedMouseOver(UIMouseEvent evt, UIElement listeningElement)
    {
        SoundEngine.PlaySound(SoundID.MenuTick);
        ((UIPanel) evt.Target).BackgroundColor = new Color(73, 94, 171);
        ((UIPanel) evt.Target).BorderColor = Colors.FancyUIFatButtonMouseOver;
    }

    private void FadedMouseOut(UIMouseEvent evt, UIElement listeningElement)
    {
        ((UIPanel) evt.Target).BackgroundColor = new Color(63, 82, 151) * 0.8f;
        ((UIPanel) evt.Target).BorderColor = Color.Black;
    }

    #endregion
}