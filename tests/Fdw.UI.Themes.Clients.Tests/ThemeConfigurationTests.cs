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
    public void CreateDefaultLightReturnsLightTheme()
    {
        var theme = ThemeConfiguration.CreateDefaultLight();

        theme.Name.ShouldBe("default-light");
        theme.DisplayName.ShouldBe("Default Light");
        theme.Description.ShouldBe("Standard light theme");
        theme.IsDefault.ShouldBeTrue();
        theme.IsDarkMode.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P3")]
    [Trait("Category", "Ui")]
    public void CreateDefaultDarkReturnsDarkTheme()
    {
        var theme = ThemeConfiguration.CreateDefaultDark();

        theme.Name.ShouldBe("default-dark");
        theme.DisplayName.ShouldBe("Default Dark");
        theme.Description.ShouldBe("Standard dark theme");
        theme.IsDarkMode.ShouldBeTrue();
        theme.IsDefault.ShouldBeFalse();
        theme.PrimaryColor.ShouldBe("#90CAF9");
        theme.SecondaryColor.ShouldBe("#CE93D8");
        theme.TertiaryColor.ShouldBe("#FFB74D");
        theme.BackgroundColor.ShouldBe("#121212");
        theme.SurfaceColor.ShouldBe("#1E1E1E");
        theme.ErrorColor.ShouldBe("#EF5350");
        theme.WarningColor.ShouldBe("#FFB74D");
        theme.SuccessColor.ShouldBe("#66BB6A");
        theme.InfoColor.ShouldBe("#42A5F5");
        theme.TextPrimary.ShouldBe("#E0E0E0");
        theme.TextSecondary.ShouldBe("#9E9E9E");
        theme.TextDisabled.ShouldBe("#616161");
        theme.TextOnPrimary.ShouldBe("#000000");
        theme.TextOnSecondary.ShouldBe("#000000");
    }

    [Fact]
    [Trait("Priority", "P3")]
    [Trait("Category", "Ui")]
    public void CreateFractalThemeReturnsFractalBrandTheme()
    {
        var theme = ThemeConfiguration.CreateFractalTheme();

        theme.Name.ShouldBe("fractal");
        theme.DisplayName.ShouldBe("Fractal");
        theme.Description.ShouldBe("Deep Logic Purple - Fdw brand theme");
        theme.IsDarkMode.ShouldBeTrue();
        theme.IsDefault.ShouldBeFalse();
        theme.PrimaryColor.ShouldBe("#7209B7");
        theme.SecondaryColor.ShouldBe("#3A0CA3");
        theme.TertiaryColor.ShouldBe("#F72585");
        theme.BackgroundColor.ShouldBe("#0F1115");
        theme.SurfaceColor.ShouldBe("#1A1D24");
        theme.ErrorColor.ShouldBe("#FF6B6B");
        theme.WarningColor.ShouldBe("#FFE66D");
        theme.SuccessColor.ShouldBe("#4ECDC4");
        theme.InfoColor.ShouldBe("#4EA8DE");
        theme.TextPrimary.ShouldBe("#E2E8F0");
        theme.TextSecondary.ShouldBe("#94A3B8");
        theme.TextDisabled.ShouldBe("#64748B");
        theme.TextOnPrimary.ShouldBe("#FFFFFF");
        theme.TextOnSecondary.ShouldBe("#FFFFFF");
    }

    [Fact]
    [Trait("Priority", "P3")]
    [Trait("Category", "Ui")]
    public void DefaultConstructorSetsExpectedDefaults()
    {
        var theme = new ThemeConfiguration();

        theme.Id.ShouldNotBe(Guid.Empty);
        theme.Name.ShouldBe(string.Empty);
        theme.DisplayName.ShouldBeNull();
        theme.Description.ShouldBeNull();
        theme.PrimaryColor.ShouldBe("#1976D2");
        theme.SecondaryColor.ShouldBe("#424242");
        theme.TertiaryColor.ShouldBe("#7B1FA2");
        theme.BackgroundColor.ShouldBe("#FFFFFF");
        theme.SurfaceColor.ShouldBe("#F5F5F5");
        theme.ErrorColor.ShouldBe("#D32F2F");
        theme.WarningColor.ShouldBe("#FFA000");
        theme.SuccessColor.ShouldBe("#388E3C");
        theme.InfoColor.ShouldBe("#1976D2");
        theme.TextPrimary.ShouldBe("#212121");
        theme.TextSecondary.ShouldBe("#757575");
        theme.TextDisabled.ShouldBe("#9E9E9E");
        theme.TextOnPrimary.ShouldBe("#FFFFFF");
        theme.TextOnSecondary.ShouldBe("#FFFFFF");
        theme.FontFamily.ShouldBe("Roboto, sans-serif");
        theme.FontFamilyMono.ShouldBe("JetBrains Mono, Consolas, monospace");
        theme.FontSizeBase.ShouldBe(14);
        theme.BorderRadius.ShouldBe(4);
        theme.IsDarkMode.ShouldBeFalse();
        theme.IsDefault.ShouldBeFalse();
        theme.LogoUrl.ShouldBeNull();
        theme.AppName.ShouldBe("Fdw");
        theme.FaviconUrl.ShouldBeNull();
    }
}
