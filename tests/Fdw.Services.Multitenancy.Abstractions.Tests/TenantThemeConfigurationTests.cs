using Fdw.Services.Multitenancy.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Multitenancy.Abstractions.Tests;

public class TenantThemeConfigurationTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ConstructorSetsDefaultHSLColors()
    {
        // Act
        var result = new TenantThemeConfiguration();

        // Assert
        result.PrimaryColor.ShouldBe("221 83% 53%");
        result.SecondaryColor.ShouldBe("215 16% 47%");
        result.AccentColor.ShouldBe("262 83% 58%");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void DarkModeDefaultDefaultsToTrue()
    {
        // Act
        var result = new TenantThemeConfiguration();

        // Assert
        result.DarkModeDefault.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ColorsCanBeSet()
    {
        // Arrange
        var config = new TenantThemeConfiguration();
        var custom = "10 20% 30%";

        // Act
        config.PrimaryColor = custom;
        config.SecondaryColor = custom;
        config.AccentColor = custom;
        config.BackgroundColor = custom;
        config.SurfaceColor = custom;
        config.OverlayColor = custom;
        config.SuccessColor = custom;
        config.WarningColor = custom;
        config.ErrorColor = custom;
        config.InfoColor = custom;
        config.TextMainColor = custom;
        config.TextMutedColor = custom;

        // Assert
        config.PrimaryColor.ShouldBe(custom);
        config.TextMutedColor.ShouldBe(custom);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ToThemeCopiesAll12Colors()
    {
        // Arrange
        var config = new TenantThemeConfiguration
        {
            PrimaryColor = "1 2% 3%",
            SecondaryColor = "4 5% 6%",
            AccentColor = "7 8% 9%",
            BackgroundColor = "10 11% 12%",
            SurfaceColor = "13 14% 15%",
            OverlayColor = "16 17% 18%",
            SuccessColor = "19 20% 21%",
            WarningColor = "22 23% 24%",
            ErrorColor = "25 26% 27%",
            InfoColor = "28 29% 30%",
            TextMainColor = "31 32% 33%",
            TextMutedColor = "34 35% 36%"
        };

        // Act
        var result = config.ToTheme();

        // Assert
        result.PrimaryColor.ShouldBe("1 2% 3%");
        result.SecondaryColor.ShouldBe("4 5% 6%");
        result.AccentColor.ShouldBe("7 8% 9%");
        result.BackgroundColor.ShouldBe("10 11% 12%");
        result.SurfaceColor.ShouldBe("13 14% 15%");
        result.OverlayColor.ShouldBe("16 17% 18%");
        result.SuccessColor.ShouldBe("19 20% 21%");
        result.WarningColor.ShouldBe("22 23% 24%");
        result.ErrorColor.ShouldBe("25 26% 27%");
        result.InfoColor.ShouldBe("28 29% 30%");
        result.TextMainColor.ShouldBe("31 32% 33%");
        result.TextMutedColor.ShouldBe("34 35% 36%");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ToThemeCopiesOtherProperties()
    {
        // Arrange
        var config = new TenantThemeConfiguration
        {
            LogoUrl = "logo",
            FaviconUrl = "favicon",
            CustomCssUrl = "css",
            DarkModeDefault = false
        };

        // Act
        var result = config.ToTheme();

        // Assert
        result.LogoUrl.ShouldBe("logo");
        result.FaviconUrl.ShouldBe("favicon");
        result.CustomCssUrl.ShouldBe("css");
        result.DarkModeDefault.ShouldBeFalse();
    }
}