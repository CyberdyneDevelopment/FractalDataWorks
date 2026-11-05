using Fdw.UI.Themes.Clients.Models;

namespace Fdw.UI.Themes.Clients.Tests;

public sealed class ThemeConfigurationTests
{
    [Fact]
    [Trait("Priority", "P3")]
    [Trait("Category", "Ui")]
    public void CloneCreatesDeepCopy()
    {
        var original = new ThemeConfiguration
        {
            Name = "test",
            DisplayName = "Test Theme",
            Description = "A test theme",
            PrimaryColor = "#FF0000",
            SecondaryColor = "#00FF00",
            TertiaryColor = "#0000FF",
            BackgroundColor = "#111111",
            SurfaceColor = "#222222",
            ErrorColor = "#EE0000",
            WarningColor = "#FFAA00",
            SuccessColor = "#00AA00",
            InfoColor = "#0000AA",
            TextPrimary = "#333333",
            TextSecondary = "#444444",
            TextDisabled = "#555555",
            TextOnPrimary = "#FFFFFF",
            TextOnSecondary = "#EEEEEE",
            FontFamily = "Arial",
            FontFamilyMono = "Courier",
            FontSizeBase = 16,
            BorderRadius = 8,
            IsDarkMode = true,
            IsDefault = false,
            LogoUrl = "https://example.com/logo.png",
            AppName = "TestApp",
            FaviconUrl = "https://example.com/favicon.ico"
        };

        var clone = original.Clone();

        clone.ShouldNotBeSameAs(original);
        clone.Id.ShouldBe(original.Id);
        clone.Name.ShouldBe(original.Name);
        clone.DisplayName.ShouldBe(original.DisplayName);
        clone.Description.ShouldBe(original.Description);
        clone.PrimaryColor.ShouldBe(original.PrimaryColor);
        clone.SecondaryColor.ShouldBe(original.SecondaryColor);
        clone.TertiaryColor.ShouldBe(original.TertiaryColor);
        clone.BackgroundColor.ShouldBe(original.BackgroundColor);
        clone.SurfaceColor.ShouldBe(original.SurfaceColor);
        clone.ErrorColor.ShouldBe(original.ErrorColor);
        clone.WarningColor.ShouldBe(original.WarningColor);
        clone.SuccessColor.ShouldBe(original.SuccessColor);
        clone.InfoColor.ShouldBe(original.InfoColor);
        clone.TextPrimary.ShouldBe(original.TextPrimary);
        clone.TextSecondary.ShouldBe(original.TextSecondary);
        clone.TextDisabled.ShouldBe(original.TextDisabled);
        clone.TextOnPrimary.ShouldBe(original.TextOnPrimary);
        clone.TextOnSecondary.ShouldBe(original.TextOnSecondary);
        clone.FontFamily.ShouldBe(original.FontFamily);
        clone.FontFamilyMono.ShouldBe(original.FontFamilyMono);
        clone.FontSizeBase.ShouldBe(original.FontSizeBase);
        clone.BorderRadius.ShouldBe(original.BorderRadius);
        clone.IsDarkMode.ShouldBe(original.IsDarkMode);
        clone.IsDefault.ShouldBe(original.IsDefault);
        clone.LogoUrl.ShouldBe(original.LogoUrl);
        clone.AppName.ShouldBe(original.AppName);
        clone.FaviconUrl.ShouldBe(original.FaviconUrl);
    }

    [Fact]
    [Trait("Priority", "P3")]
    [Trait("Category", "Ui")]
    public void DefaultConstructorStatesNoValue()
    {
        var theme = new ThemeConfiguration();

        // A theme's values come from the server (ToDto) or a seeded setting, never from the type (FDW-3823).
        theme.Id.ShouldBe(Guid.Empty);
        theme.Name.ShouldBe(string.Empty);
        theme.DisplayName.ShouldBeNull();
        theme.Description.ShouldBeNull();
        theme.PrimaryColor.ShouldBe(string.Empty);
        theme.SecondaryColor.ShouldBe(string.Empty);
        theme.BackgroundColor.ShouldBe(string.Empty);
        theme.FontFamily.ShouldBe(string.Empty);
        theme.FontSizeBase.ShouldBe(0);
        theme.BorderRadius.ShouldBe(0);
        theme.IsDarkMode.ShouldBeFalse();
        theme.IsDefault.ShouldBeFalse();
        theme.LogoUrl.ShouldBeNull();
        theme.AppName.ShouldBe(string.Empty);
        theme.FaviconUrl.ShouldBeNull();
    }
}
